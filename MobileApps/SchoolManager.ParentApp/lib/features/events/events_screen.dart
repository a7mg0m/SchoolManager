import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_format.dart';
import 'events_service.dart';

enum _Tab { upcoming, past }

/// School events of the child selected on home: for everyone, its stage, grade or class, or the child itself.
/// Upcoming (with the next event highlighted) or past, grouped by month; tap an event for its details.
class EventsScreen extends StatefulWidget {
  const EventsScreen({super.key, required this.child, this.loadEvents, this.onSessionExpired, this.now});

  final Child child;

  /// Defaults to the API (tests pass their own)
  final Future<List<ChildEvent>> Function(String studentId)? loadEvents;

  /// Called when the token was rejected (the page closes first)
  final VoidCallback? onSessionExpired;

  /// Current time (tests pass a fixed one)
  final DateTime? now;

  @override
  State<EventsScreen> createState() => _EventsScreenState();
}

class _EventsScreenState extends State<EventsScreen> {
  static const _muted = Color(0xFF637394);
  static const _pink = Color(0xFFE0457B);
  static const _green = Color(0xFF13864B);
  static const _red = Color(0xFFD93025);

  List<ChildEvent>? _events;
  String? _error;
  bool _loading = true;
  _Tab _tab = _Tab.upcoming;

  DateTime get _now => widget.now ?? DateTime.now();

  /// Colour of each audience (badge on the cards)
  static Color audienceColor(EventAudience a) => switch (a) {
        EventAudience.all => const Color(0xFF1677E8),
        EventAudience.stage => const Color(0xFF0FA5A5),
        EventAudience.grade => const Color(0xFF8957E5),
        EventAudience.section => const Color(0xFFF08C00),
        EventAudience.teacher => const Color(0xFF637394),
        EventAudience.student => _pink,
      };

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _events == null;
      _error = null;
    });
    try {
      final loader = widget.loadEvents ?? EventsService.instance.getChildEvents;
      final result = await loader(widget.child.id);
      if (!mounted) return;
      setState(() {
        _events = result;
        _loading = false;
      });
    } on EventsException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired) {
        Navigator.of(context).pop();
        widget.onSessionExpired?.call();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  static int _byDateTime(ChildEvent a, ChildEvent b) {
    final d = a.date.compareTo(b.date);
    return d != 0 ? d : a.start.compareTo(b.start);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('الفعاليات', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
          ],
        ),
      ),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _centerMessage(Icons.error_outline_rounded, _error!, retry: true)
                : _content(_events!),
      ),
    );
  }

  Widget _content(List<ChildEvent> events) {
    final upcoming = events.where((e) => e.isUpcoming(_now)).toList()..sort(_byDateTime);
    final past = events.where((e) => !e.isUpcoming(_now)).toList()..sort((a, b) => _byDateTime(b, a));
    final shown = _tab == _Tab.upcoming ? upcoming : past;

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 16, 6),
          child: SizedBox(
            width: double.infinity,
            child: SegmentedButton<_Tab>(
              segments: [
                ButtonSegment(value: _Tab.upcoming, icon: const Icon(Icons.event_available_rounded), label: Text('القادمة (${upcoming.length})')),
                ButtonSegment(value: _Tab.past, icon: const Icon(Icons.history_rounded), label: Text('السابقة (${past.length})')),
              ],
              selected: {_tab},
              showSelectedIcon: false,
              onSelectionChanged: (s) => setState(() => _tab = s.first),
            ),
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: _load,
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 24),
              children: [
                if (shown.isEmpty)
                  Padding(
                    padding: const EdgeInsets.only(top: 60),
                    child: _centerMessage(
                      Icons.celebration_outlined,
                      _tab == _Tab.upcoming ? 'لا توجد فعاليات قادمة حالياً.' : 'لا توجد فعاليات سابقة.',
                    ),
                  ),
                // Next event in a big card, the rest grouped by month
                if (_tab == _Tab.upcoming && upcoming.isNotEmpty) ...[
                  _nextEventCard(upcoming.first),
                  const SizedBox(height: 18),
                ],
                ..._byMonth(_tab == _Tab.upcoming && upcoming.isNotEmpty ? upcoming.skip(1).toList() : shown),
              ],
            ),
          ),
        ),
      ],
    );
  }

  /// Month headers ("نوفمبر 2026") followed by their events
  List<Widget> _byMonth(List<ChildEvent> events) {
    final widgets = <Widget>[];
    String? current;
    for (final event in events) {
      final month = '${arabicMonths[event.date.month - 1]} ${event.date.year}';
      if (month != current) {
        current = month;
        widgets.add(Padding(
          padding: EdgeInsets.only(bottom: 10, top: widgets.isEmpty ? 0 : 8),
          child: Row(
            children: [
              const Icon(Icons.calendar_month_rounded, size: 18, color: _muted),
              const SizedBox(width: 6),
              Text(month, style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: AppTheme.ink)),
            ],
          ),
        ));
      }
      widgets.add(_eventCard(event));
    }
    return widgets;
  }

  /// The next upcoming event: big gradient card with a countdown
  Widget _nextEventCard(ChildEvent event) {
    final isNow = event.isNow(_now);
    return InkWell(
      borderRadius: BorderRadius.circular(22),
      onTap: () => _showDetails(event),
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          gradient: const LinearGradient(
            colors: [Color(0xFFE0457B), Color(0xFF8957E5)],
            begin: Alignment.topRight,
            end: Alignment.bottomLeft,
          ),
          borderRadius: BorderRadius.circular(22),
          boxShadow: [BoxShadow(color: _pink.withValues(alpha: .25), blurRadius: 14, offset: const Offset(0, 6))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Wrap(
              spacing: 8,
              runSpacing: 6,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                _whiteChip(isNow ? 'الآن' : 'الفعالية القادمة', isNow ? Icons.play_arrow_rounded : Icons.star_rounded),
                _whiteChip(event.audience.label, Icons.groups_rounded),
              ],
            ),
            const SizedBox(height: 12),
            Text(event.title, style: const TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold)),
            if (event.description.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(event.description,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Colors.white70, fontSize: 13)),
            ],
            const SizedBox(height: 14),
            Wrap(
              spacing: 14,
              runSpacing: 6,
              children: [
                _whiteMeta(Icons.event_rounded, _dateText(event.date)),
                _whiteMeta(Icons.schedule_rounded, '${formatTime(event.start)} - ${formatTime(event.end)}'),
              ],
            ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(20)),
              child: Text(_countdown(event),
                  style: const TextStyle(color: _pink, fontWeight: FontWeight.bold, fontSize: 12.5)),
            ),
          ],
        ),
      ),
    );
  }

  /// One event: date block, title, time, audience
  Widget _eventCard(ChildEvent event) {
    final isPast = !event.isUpcoming(_now);
    final color = isPast ? const Color(0xFFA0AABF) : audienceColor(event.audience);
    final isNow = event.isNow(_now);

    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Material(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        child: InkWell(
          borderRadius: BorderRadius.circular(18),
          onTap: () => _showDetails(event),
          child: Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(18),
              border: Border.all(color: isNow ? _green.withValues(alpha: .5) : const Color(0xFFE6ECF5)),
            ),
            child: Row(
              children: [
                _dateBlock(event.date, color),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(event.title,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: TextStyle(
                            fontSize: 15.5,
                            fontWeight: FontWeight.bold,
                            color: isPast ? _muted : AppTheme.ink,
                          )),
                      const SizedBox(height: 4),
                      _meta(Icons.schedule_rounded, '${formatTime(event.start)} - ${formatTime(event.end)}'),
                      const SizedBox(height: 6),
                      Wrap(
                        spacing: 6,
                        runSpacing: 4,
                        children: [
                          _chip(event.audience.label, color),
                          if (isNow) _chip('الآن', _green),
                          if (!isPast && !isNow) _chip(_countdown(event), AppTheme.blue),
                        ],
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_left_rounded, color: _muted),
              ],
            ),
          ),
        ),
      ),
    );
  }

  /// Bottom sheet with the full description and the image
  void _showDetails(ChildEvent event) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (sheet) => DraggableScrollableSheet(
        expand: false,
        initialChildSize: .6,
        minChildSize: .4,
        maxChildSize: .92,
        builder: (context, controller) => ListView(
          controller: controller,
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
          children: [
            if (event.imageUrl != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 14),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(16),
                  child: Image.network(
                    event.imageUrl!,
                    height: 180,
                    fit: BoxFit.cover,
                    // A broken or missing image is simply not shown
                    errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                  ),
                ),
              ),
            Text(event.title, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: AppTheme.ink)),
            const SizedBox(height: 10),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                _chip(event.audience.label, audienceColor(event.audience)),
                if (event.isNow(_now)) _chip('الآن', _green),
              ],
            ),
            const SizedBox(height: 14),
            _detailRow(Icons.event_rounded, _dateText(event.date)),
            _detailRow(Icons.schedule_rounded, '${formatTime(event.start)} - ${formatTime(event.end)}'),
            if (event.isUpcoming(_now) && !event.isNow(_now)) _detailRow(Icons.hourglass_top_rounded, _countdown(event)),
            if (event.description.isNotEmpty) ...[
              const Divider(height: 28),
              Text(event.description, style: const TextStyle(fontSize: 14.5, height: 1.7, color: AppTheme.ink)),
            ],
          ],
        ),
      ),
    );
  }

  Widget _detailRow(IconData icon, String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        children: [
          Icon(icon, size: 18, color: AppTheme.blue),
          const SizedBox(width: 8),
          Expanded(child: Text(text, style: const TextStyle(fontSize: 14, color: AppTheme.ink))),
        ],
      ),
    );
  }

  Widget _dateBlock(DateTime date, Color color) {
    return Container(
      width: 58,
      padding: const EdgeInsets.symmetric(vertical: 8),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(14)),
      child: Column(
        children: [
          Text(arabicDays[date.weekday % 7], style: TextStyle(fontSize: 10.5, color: color, fontWeight: FontWeight.bold)),
          Text('${date.day}', style: TextStyle(fontSize: 22, height: 1.15, fontWeight: FontWeight.bold, color: color)),
          Text(arabicMonths[date.month - 1], style: TextStyle(fontSize: 10.5, color: color)),
        ],
      ),
    );
  }

  Widget _chip(String text, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
      child: Text(text, style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: color)),
    );
  }

  Widget _whiteChip(String text, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
      decoration: BoxDecoration(color: Colors.white24, borderRadius: BorderRadius.circular(20)),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: Colors.white),
          const SizedBox(width: 4),
          Text(text, style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _whiteMeta(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 16, color: Colors.white),
        const SizedBox(width: 4),
        Text(text, style: const TextStyle(color: Colors.white, fontSize: 13)),
      ],
    );
  }

  Widget _meta(IconData icon, String text) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: _muted),
        const SizedBox(width: 3),
        Flexible(child: Text(text, overflow: TextOverflow.ellipsis, style: const TextStyle(fontSize: 12, color: _muted))),
      ],
    );
  }

  /// "اليوم - تبدأ بعد ساعة" / "غداً" / "بعد 5 أيام"
  String _countdown(ChildEvent event) {
    final now = _now;
    final today = DateTime(now.year, now.month, now.day);
    final days = event.date.difference(today).inDays;
    if (days == 0) {
      if (event.isNow(now)) return 'جارية الآن';
      final minutes = event.start - (now.hour * 60 + now.minute);
      return minutes > 0 ? 'اليوم - تبدأ ${formatIn(minutes)}' : 'اليوم';
    }
    if (days == 1) return 'غداً';
    if (days == 2) return 'بعد يومين';
    if (days <= 10) return 'بعد $days أيام';
    return 'بعد $days يوماً';
  }

  /// "الخميس 20 نوفمبر 2026"
  static String _dateText(DateTime d) => '${arabicDays[d.weekday % 7]} ${d.day} ${arabicMonths[d.month - 1]} ${d.year}';

  Widget _centerMessage(IconData icon, String text, {bool retry = false}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry ? _red : AppTheme.blue.withValues(alpha: .6)),
            const SizedBox(height: 12),
            Text(text, textAlign: TextAlign.center, style: const TextStyle(color: AppTheme.ink)),
            if (retry) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: _load, child: const Text('إعادة المحاولة')),
            ],
          ],
        ),
      ),
    );
  }
}
