import 'package:flutter/material.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'notifications_service.dart';

const _red = Color(0xFFD93025);
const _green = Color(0xFF13864B);

const _arDays = ['الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت', 'الأحد'];
const _enDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const _arMonths = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
const _enMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

/// "الخميس 20 نوفمبر 2026"
String noticeDate(DateTime d) {
  final en = AppLanguage.instance.isEnglish;
  return '${(en ? _enDays : _arDays)[d.weekday - 1]} ${d.day} ${(en ? _enMonths : _arMonths)[d.month - 1]} ${d.year}';
}

/// "اليوم", "أمس", else the date
String noticeDay(DateTime d, DateTime now) {
  final today = DateTime(now.year, now.month, now.day);
  if (d == today) return tr('اليوم', 'Today');
  if (d == today.subtract(const Duration(days: 1))) return tr('أمس', 'Yesterday');
  return noticeDate(d);
}

/// Notifications from the school (for everyone or for my bus), newest first, grouped by day; filter by type, tap for details
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({
    super.key,
    required this.loadNotices,
    this.subtitle,
    this.targetedLabel,
    this.onSessionExpired,
    this.now,
  });

  /// Loads the notifications (the API for the child / the teacher; tests pass their own)
  final Future<List<AppNotice>> Function() loadNotices;

  /// Under the title (the child's name in the parent app)
  final String? subtitle;

  /// Badge of a notification that is not for everyone ("لابنك" / "لك")
  final String? targetedLabel;
  final Future<void> Function()? onSessionExpired;
  final DateTime? now;

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<AppNotice>? _notices;
  String? _error;
  bool _loading = true;

  /// Type filter; null = all
  NoticeType? _type;

  DateTime get _now => widget.now ?? DateTime.now();

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _notices == null;
      _error = null;
    });
    try {
      final result = await widget.loadNotices();
      if (!mounted) return;
      setState(() {
        _notices = result;
        _loading = false;
      });
    } on NoticesException catch (e) {
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

  void _open(AppNotice notice) {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => NotificationDetailsScreen(notice: notice, targetedLabel: widget.targetedLabel, now: widget.now),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final subtitle = widget.subtitle ?? '';

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الإشعارات', 'Notifications'), style: const TextStyle(fontWeight: FontWeight.bold)),
            if (subtitle.isNotEmpty) Text(subtitle, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _message(Icons.error_outline_rounded, _error!, retry: true)
              : RefreshIndicator(onRefresh: _load, child: _content(_notices!)),
    );
  }

  Widget _content(List<AppNotice> all) {
    final palette = context.palette;
    if (all.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 100),
          _message(Icons.notifications_none_rounded,
              tr('لا توجد إشعارات بعد.', 'No notifications yet.')),
        ],
      );
    }

    final types = NoticeType.values.where((t) => all.any((n) => n.type == t)).toList();
    final shown = _type == null ? all : all.where((n) => n.type == _type).toList();
    final todayCount = all.where((n) => n.isToday(_now)).length;

    final items = <Widget>[];
    DateTime? day;
    for (final n in shown) {
      if (n.sentOn != day) {
        day = n.sentOn;
        items.add(Padding(
          padding: EdgeInsets.only(top: items.isEmpty ? 4 : 14, bottom: 8),
          child: Text(noticeDay(n.sentOn, _now), style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13.5, color: palette.muted)),
        ));
      }
      items.add(_card(n));
      items.add(const SizedBox(height: 10));
    }

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      children: [
        if (todayCount > 0)
          Container(
            margin: const EdgeInsets.only(bottom: 12),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(color: AppTheme.blue.withValues(alpha: .1), borderRadius: BorderRadius.circular(14)),
            child: Row(
              children: [
                const Icon(Icons.notifications_active_rounded, color: AppTheme.blue, size: 20),
                const SizedBox(width: 8),
                Text(tr('$todayCount إشعار جديد اليوم', '$todayCount new today'),
                    style: const TextStyle(color: AppTheme.blue, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        if (types.length > 1)
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                _typeChip(null, tr('الكل', 'All'), all.length),
                for (final t in types) _typeChip(t, t.label, all.where((n) => n.type == t).length),
              ],
            ),
          ),
        const SizedBox(height: 4),
        ...items,
      ],
    );
  }

  Widget _typeChip(NoticeType? type, String label, int count) => Padding(
        padding: const EdgeInsetsDirectional.only(end: 8),
        child: ChoiceChip(
          key: ValueKey('notice-type-${type?.name ?? 'all'}'),
          avatar: type == null ? null : Icon(type.icon, size: 16, color: type.color),
          label: Text('$label ($count)'),
          selected: _type == type,
          onSelected: (_) => setState(() => _type = type),
        ),
      );

  Widget _card(AppNotice n) {
    final palette = context.palette;
    final isNew = n.isToday(_now);
    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        key: ValueKey('notice-${n.id}'),
        borderRadius: BorderRadius.circular(18),
        onTap: () => _open(n),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: isNew ? n.type.color.withValues(alpha: .45) : palette.line),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(color: n.type.color.withValues(alpha: .12), borderRadius: BorderRadius.circular(14)),
                child: Icon(n.type.icon, color: n.type.color),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(n.title, maxLines: 1, overflow: TextOverflow.ellipsis,
                              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15, color: palette.ink)),
                        ),
                        if (isNew) _badge(tr('جديد', 'New'), _green),
                      ],
                    ),
                    if (n.description.isNotEmpty) ...[
                      const SizedBox(height: 3),
                      Text(n.description, maxLines: 2, overflow: TextOverflow.ellipsis,
                          style: TextStyle(fontSize: 13, height: 1.4, color: palette.muted)),
                    ],
                    const SizedBox(height: 8),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      children: [
                        _badge(n.type.label, n.type.color),
                        _badge(n.forEveryone ? tr('للجميع', 'Everyone') : (widget.targetedLabel ?? tr('خاص', 'Targeted')),
                            n.forEveryone ? palette.muted : AppTheme.blue),
                        if (n.imageUrl != null) Icon(Icons.image_outlined, size: 16, color: palette.muted),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _badge(String text, Color color) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
        child: Text(text, style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: color)),
      );

  Widget _message(IconData icon, String text, {bool retry = false}) => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 56, color: retry ? _red : AppTheme.blue.withValues(alpha: .6)),
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

/// One notification on its own page: type, title, date, who it is for, the picture and the full text
class NotificationDetailsScreen extends StatelessWidget {
  const NotificationDetailsScreen({super.key, required this.notice, this.targetedLabel, this.now});

  final AppNotice notice;
  final String? targetedLabel;
  final DateTime? now;

  /// Header: the picture (tap = full screen) under a dark fade for the title; the coloured header [colored]
  /// on top of it is kept for the title / type. No picture, still loading or broken: only the coloured header.
  static Widget _headerBackground(BuildContext context, AppNotice n, Widget colored) {
    final url = n.imageUrl;
    if (url == null) return colored;
    return Stack(
      fit: StackFit.expand,
      children: [
        colored,
        GestureDetector(
          key: const ValueKey('notice-image'),
          // The whole header is tappable (also while the picture loads)
          behavior: HitTestBehavior.opaque,
          onTap: () => _openImage(context, url),
          child: Image.network(
            url,
            fit: BoxFit.cover,
            // While loading / when it cannot be loaded: the coloured header shows through
            loadingBuilder: (context, child, progress) => progress == null ? child : const SizedBox.shrink(),
            errorBuilder: (context, error, stackTrace) => const SizedBox.shrink(),
          ),
        ),
        // Dark fade (top for the back button, bottom for the title) so the text stays readable
        IgnorePointer(
          child: DecoratedBox(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                stops: const [0, .4, 1],
                colors: [Colors.black.withValues(alpha: .35), Colors.transparent, Colors.black.withValues(alpha: .75)],
              ),
            ),
          ),
        ),
        // The title and type again, above the picture
        IgnorePointer(child: _titleOnly(colored)),
      ],
    );
  }

  /// The coloured header without its own background (only its texts), drawn over the picture
  static Widget _titleOnly(Widget colored) =>
      colored is Container && colored.child != null ? colored.child! : const SizedBox.shrink();

  /// The picture full screen: pinch to zoom, tap or back to close
  static void _openImage(BuildContext context, String url) {
    Navigator.of(context).push(PageRouteBuilder<void>(
      opaque: false,
      barrierColor: Colors.black87,
      pageBuilder: (context, animation, secondaryAnimation) => GestureDetector(
        onTap: () => Navigator.of(context).pop(),
        child: Scaffold(
          backgroundColor: Colors.transparent,
          appBar: AppBar(backgroundColor: Colors.transparent, foregroundColor: Colors.white, elevation: 0),
          body: Center(
            child: InteractiveViewer(
              maxScale: 4,
              child: Image.network(
                url,
                key: const ValueKey('notice-image-full'),
                fit: BoxFit.contain,
                errorBuilder: (context, error, stackTrace) =>
                    const Icon(Icons.broken_image_outlined, color: Colors.white54, size: 64),
              ),
            ),
          ),
        ),
      ),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final n = notice;
    final color = n.type.color;

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            pinned: true,
            // The picture is the header: a taller one when there is a picture
            expandedHeight: n.imageUrl != null ? 300 : 200,
            foregroundColor: Colors.white,
            backgroundColor: color,
            flexibleSpace: FlexibleSpaceBar(
              background: _headerBackground(context, n, Container(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [color.withValues(alpha: .75), color],
                    begin: AlignmentDirectional.topStart,
                    end: AlignmentDirectional.bottomEnd,
                  ),
                ),
                child: Stack(
                  children: [
                    PositionedDirectional(
                      end: -24,
                      top: 24,
                      child: Icon(n.type.icon, size: 160, color: Colors.white.withValues(alpha: .12)),
                    ),
                    SafeArea(
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(20, 56, 20, 18),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          mainAxisAlignment: MainAxisAlignment.end,
                          children: [
                            Row(
                              children: [
                                Icon(n.type.icon, color: Colors.white, size: 18),
                                const SizedBox(width: 6),
                                Text(n.type.label,
                                    style: TextStyle(color: Colors.white.withValues(alpha: .9), fontWeight: FontWeight.w600)),
                              ],
                            ),
                            const SizedBox(height: 6),
                            Text(n.title,
                                maxLines: 3,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold, height: 1.3)),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              )),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 28),
            sliver: SliverList.list(
              children: [
                Row(
                  children: [
                    Expanded(child: _fact(context, Icons.event_rounded, tr('التاريخ', 'Date'), noticeDay(n.sentOn, now ?? DateTime.now()), color)),
                    const SizedBox(width: 10),
                    Expanded(
                      child: _fact(
                        context,
                        n.forEveryone ? Icons.public_rounded : Icons.person_pin_rounded,
                        tr('موجه إلى', 'For'),
                        n.forEveryone ? tr('الجميع', 'Everyone') : (targetedLabel ?? tr('خاص', 'Targeted')),
                        AppTheme.blue,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: palette.card,
                    borderRadius: BorderRadius.circular(18),
                    border: Border.all(color: palette.line),
                  ),
                  child: SelectableText(
                    n.description.isEmpty ? tr('لا يوجد تفاصيل إضافية.', 'No more details.') : n.description,
                    style: TextStyle(fontSize: 15, height: 1.8, color: n.description.isEmpty ? palette.muted : palette.ink),
                  ),
                ),
                const SizedBox(height: 10),
                Text(noticeDate(n.sentOn), textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: palette.muted)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _fact(BuildContext context, IconData icon, String label, String value, Color color) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
      child: Row(
        children: [
          Icon(icon, color: color, size: 22),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(fontSize: 11, color: palette.muted)),
                Text(value, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: palette.ink)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
