import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../chat/chat_screens.dart';
import '../chat/chat_service.dart';
import 'parents_service.dart';
import '../../core/live/live_refresh.dart';
import '../../core/widgets/paged.dart';

const _amber = Color(0xFFB7791F);
const _green = Color(0xFF13864B);
const _red = Color(0xFFD93025);

/// Parents of the students in the teacher's classes: filter by class, search, call / email
class ParentsScreen extends StatefulWidget {
  const ParentsScreen({
    super.key,
    this.loadParents,
    this.onSessionExpired,
    this.openUri,
    this.chatApi = const ChatApi(),
    this.chatRefreshEvery = const Duration(seconds: 5),
  });

  /// Chat calls (start / open the chat with a parent); defaults to the API
  final ChatApi chatApi;

  /// How often an open chat fetches new messages (null: never; tests)
  final Duration? chatRefreshEvery;

  /// Defaults to the API (tests pass their own)
  final Future<List<TeacherParent>> Function()? loadParents;
  final Future<void> Function()? onSessionExpired;

  /// Opens tel: / mailto: links; defaults to the system (tests pass their own)
  final Future<bool> Function(Uri uri)? openUri;

  @override
  State<ParentsScreen> createState() => _ParentsScreenState();
}

class _ParentsScreenState extends State<ParentsScreen> with LiveRefreshMixin {
  /// Long lists: the first items, then "Show more"
  final _pages = PageWindows();

  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  List<TeacherParent>? _parents;
  String? _error;
  bool _loading = true;
  String _query = '';

  /// Class filter (section id); null = all classes
  String? _sectionId;

  /// Parent whose chat is being opened
  String? _openingChatId;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _parents == null;
      _error = null;
    });
    try {
      final result = await (widget.loadParents ?? ParentsService.instance.getMyParents)();
      if (!mounted) return;
      setState(() {
        _parents = result;
        _loading = false;
      });
    } on ParentsException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  void _show(String text) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _open(Uri uri, String fallbackCopy) async {
    var opened = false;
    try {
      opened = await (widget.openUri ?? (u) => launchUrl(u, mode: LaunchMode.externalApplication))(uri);
    } catch (_) {}
    if (opened || !mounted) return;
    // No app for it (e.g. on the web): copy instead
    await Clipboard.setData(ClipboardData(text: fallbackCopy));
    if (mounted) _show(tr('تم النسخ: $fallbackCopy', 'Copied: $fallbackCopy'));
  }

  void _call(TeacherParent p) => _open(Uri(scheme: 'tel', path: p.phoneNumber), p.phoneNumber);
  /// Opens the chat with [p] (the parent's id is their user id)
  Future<void> _chat(TeacherParent p) async {
    setState(() => _openingChatId = p.id);
    try {
      final roomId = await widget.chatApi.startChat(p.id);
      if (!mounted) return;
      setState(() => _openingChatId = null);
      await Navigator.of(context).push(MaterialPageRoute(
        builder: (_) => ChatScreen(
          api: widget.chatApi,
          roomId: roomId,
          title: p.name,
          subtitle: tr('ولي أمر ${p.children.map((c) => c.name).join('، ')}', 'Parent of ${p.children.map((c) => c.name).join(', ')}'),
          otherUserId: p.id,
          onSessionExpired: widget.onSessionExpired,
          refreshEvery: widget.chatRefreshEvery,
        ),
      ));
    } on ChatException catch (e) {
      if (!mounted) return;
      setState(() => _openingChatId = null);
      if (e.sessionExpired && widget.onSessionExpired != null) {
        await widget.onSessionExpired!();
        return;
      }
      _show(e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(tr('أولياء الأمور', 'Parents'), style: const TextStyle(fontWeight: FontWeight.bold))),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(Icons.error_outline_rounded, _error!, retry: true)
              : RefreshIndicator(onRefresh: _load, child: _content(_parents!)),
    );
  }

  Widget _content(List<TeacherParent> parents) {
    final palette = context.palette;
    if (parents.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 100),
          _message(Icons.family_restroom_rounded,
              tr('لا يوجد أولياء أمور لطلاب فصولك.', 'There are no parents of students in your classes.')),
        ],
      );
    }

    // Classes of the children, for the filter chips
    final classes = <String, String>{};
    for (final c in parents.expand((p) => p.children)) {
      classes.putIfAbsent(c.sectionId, () => c.classLabel);
    }
    final sortedClasses = classes.entries.toList()..sort((a, b) => a.value.compareTo(b.value));
    final students = parents.expand((p) => p.children).map((c) => c.studentId).toSet().length;

    final shown = parents
        .where((p) => _sectionId == null || p.children.any((c) => c.sectionId == _sectionId))
        .where((p) => p.matches(_query))
        .toList();

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
      children: [
        Container(
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 8),
          decoration: BoxDecoration(
            gradient: const LinearGradient(colors: [Color(0xFFE0A13B), _amber]),
            borderRadius: BorderRadius.circular(22),
          ),
          child: Row(
            children: [
              _stat(Icons.family_restroom_rounded, '${parents.length}', tr('ولي أمر', 'Parents')),
              _stat(Icons.groups_rounded, '$students', tr('طالب', 'Students')),
              _stat(Icons.meeting_room_rounded, '${classes.length}', tr('فصول', 'Classes')),
            ],
          ),
        ),
        const SizedBox(height: 14),
        TextField(
          key: const ValueKey('parents-search'),
          onChanged: (v) => setState(() => _query = v),
          decoration: InputDecoration(
            hintText: tr('ابحث باسم ولي الأمر أو الطالب أو الهاتف', 'Search by parent, student or phone'),
            prefixIcon: const Icon(Icons.search_rounded),
            isDense: true,
          ),
        ),
        if (sortedClasses.length > 1) ...[
          const SizedBox(height: 10),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _classChip(null, tr('كل الفصول', 'All classes')),
                for (final c in sortedClasses) _classChip(c.key, c.value),
              ],
            ),
          ),
        ],
        const SizedBox(height: 12),
        if (shown.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 30),
            child: Text(tr('لا توجد نتائج.', 'No results.'), textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
          )
        else
          for (final p in _pages.take('parents', shown)) ...[
            _parentCard(p),
            const SizedBox(height: 10),
          ],
          _pages.more(context, 'parents', shown.length, setState),
      ],
    );
  }

  Widget _classChip(String? sectionId, String label) => Padding(
        padding: const EdgeInsetsDirectional.only(end: 8),
        child: ChoiceChip(
          key: ValueKey('parents-class-${sectionId ?? 'all'}'),
          label: Text(label),
          selected: _sectionId == sectionId,
          onSelected: (_) => setState(() => _sectionId = sectionId),
        ),
      );

  Widget _stat(IconData icon, String value, String label) => Expanded(
        child: Column(
          children: [
            Icon(icon, color: Colors.white, size: 22),
            const SizedBox(height: 4),
            Text(value, style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
            Text(label, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 12)),
          ],
        ),
      );

  Widget _parentCard(TeacherParent p) {
    final palette = context.palette;
    final initial = p.name.trim().isEmpty ? '?' : p.name.trim().characters.first;
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: palette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 22,
                backgroundColor: _amber.withValues(alpha: .14),
                child: Text(initial, style: const TextStyle(color: _amber, fontWeight: FontWeight.bold, fontSize: 18)),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(p.name, maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: palette.ink)),
                    if (p.phoneNumber.isNotEmpty)
                      Text(p.phoneNumber,
                          textDirection: TextDirection.ltr,
                          style: TextStyle(fontSize: 12.5, color: palette.muted)),
                  ],
                ),
              ),
              // Chat with this parent (opens the existing chat, or starts it)
              _openingChatId == p.id
                  ? const Padding(
                      padding: EdgeInsets.all(12),
                      child: SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2)),
                    )
                  : IconButton.filledTonal(
                      key: ValueKey('chat-${p.id}'),
                      tooltip: tr('محادثة', 'Chat'),
                      style: IconButton.styleFrom(backgroundColor: AppTheme.blue.withValues(alpha: .12)),
                      icon: const Icon(Icons.chat_bubble_outline_rounded, color: AppTheme.blue),
                      onPressed: _openingChatId == null ? () => _chat(p) : null,
                    ),
              const SizedBox(width: 4),
              if (p.phoneNumber.isNotEmpty)
                IconButton.filledTonal(
                  key: ValueKey('call-${p.id}'),
                  tooltip: tr('اتصال', 'Call'),
                  style: IconButton.styleFrom(backgroundColor: _green.withValues(alpha: .12)),
                  icon: const Icon(Icons.call_rounded, color: _green),
                  onPressed: () => _call(p),
                ),
            ],
          ),
          const SizedBox(height: 10),
          Wrap(
            spacing: 6,
            runSpacing: 6,
            children: [
              for (final c in p.children)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
                  decoration: BoxDecoration(color: palette.soft, borderRadius: BorderRadius.circular(12)),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.child_care_rounded, size: 15, color: palette.muted),
                      const SizedBox(width: 4),
                      Text(c.name, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: palette.ink)),
                      Text(' · ${c.classLabel}', style: TextStyle(fontSize: 11.5, color: palette.muted)),
                    ],
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _message(IconData icon, String text, {bool retry = false}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry ? _red : _amber.withValues(alpha: .6)),
              const SizedBox(height: 12),
              Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
              if (retry) ...[
                const SizedBox(height: 12),
                FilledButton(onPressed: _load, child: Text(tr('إعادة المحاولة', 'Try again'))),
              ],
            ],
          ),
        ),
      );
}
