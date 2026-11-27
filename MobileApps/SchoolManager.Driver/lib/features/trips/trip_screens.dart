import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'location_tracker.dart';
import 'trip_map_screen.dart';
import 'trip_service.dart';
import 'trip_widgets.dart';
import '../../core/live/live_refresh.dart';

/// Opens the "new trip" sheet; returns the trip created (date + type), or null when cancelled
Future<(DateTime, TripType)?> showNewTripSheet(
  BuildContext context, {
  required Future<void> Function(DateTime date, TripType type) create,
}) {
  return showModalBottomSheet<(DateTime, TripType)>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (_) => _NewTripSheet(create: create),
  );
}

class _NewTripSheet extends StatefulWidget {
  const _NewTripSheet({required this.create});
  final Future<void> Function(DateTime date, TripType type) create;

  @override
  State<_NewTripSheet> createState() => _NewTripSheetState();
}

class _NewTripSheetState extends State<_NewTripSheet> {
  DateTime _date = DateTime.now();

  /// Morning: to school; afternoon: to home
  TripType _type = DateTime.now().hour < 12 ? TripType.toSchool : TripType.toHome;
  bool _saving = false;
  String? _error;

  Future<void> _pickDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(now.year, now.month, now.day).subtract(const Duration(days: 7)),
      lastDate: now.add(const Duration(days: 30)),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _save() async {
    setState(() {
      _saving = true;
      _error = null;
    });
    try {
      await widget.create(_date, _type);
      if (mounted) Navigator.of(context).pop((_date, _type));
    } catch (e) {
      if (mounted) {
        setState(() {
          _saving = false;
          _error = '$e';
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 20 + MediaQuery.of(context).viewInsets.bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(tr('رحلة جديدة', 'New trip'),
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: palette.ink)),
          const SizedBox(height: 4),
          Text(tr('يُضاف إليها كل طلاب الحافلة في ذلك اليوم.', 'Every student riding the bus that day is added.'),
              style: TextStyle(color: palette.muted)),
          const SizedBox(height: 18),
          Text(tr('نوع الرحلة', 'Trip type'), style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
          const SizedBox(height: 8),
          Row(
            children: [
              for (final t in TripType.values) ...[
                Expanded(
                  child: _TypeOption(
                    key: ValueKey('type-${t.name}'),
                    type: t,
                    selected: _type == t,
                    onTap: () => setState(() => _type = t),
                  ),
                ),
                if (t == TripType.toSchool) const SizedBox(width: 10),
              ],
            ],
          ),
          const SizedBox(height: 16),
          Text(tr('التاريخ', 'Date'), style: TextStyle(fontWeight: FontWeight.w600, color: palette.ink)),
          const SizedBox(height: 8),
          InkWell(
            key: const ValueKey('trip-date'),
            borderRadius: BorderRadius.circular(14),
            onTap: _pickDate,
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
              decoration: BoxDecoration(color: palette.field, borderRadius: BorderRadius.circular(14)),
              child: Row(
                children: [
                  Icon(Icons.event_rounded, color: palette.muted),
                  const SizedBox(width: 10),
                  Expanded(child: Text(formatDay(_date), style: TextStyle(color: palette.ink, fontWeight: FontWeight.w600))),
                  Icon(Icons.edit_calendar_outlined, color: palette.muted, size: 20),
                ],
              ),
            ),
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(color: palette.errorTint, borderRadius: BorderRadius.circular(12)),
              child: Text(_error!, style: const TextStyle(color: Color(0xFFC0392B))),
            ),
          ],
          const SizedBox(height: 18),
          SizedBox(
            height: 50,
            child: FilledButton.icon(
              key: const ValueKey('create-trip'),
              onPressed: _saving ? null : _save,
              style: FilledButton.styleFrom(
                backgroundColor: AppTheme.blue,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              ),
              icon: _saving
                  ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                  : const Icon(Icons.add_road_rounded),
              label: Text(tr('إنشاء الرحلة', 'Create trip'), style: const TextStyle(fontWeight: FontWeight.bold)),
            ),
          ),
        ],
      ),
    );
  }
}

class _TypeOption extends StatelessWidget {
  const _TypeOption({super.key, required this.type, required this.selected, required this.onTap});
  final TripType type;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    return InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(vertical: 14),
        decoration: BoxDecoration(
          color: selected ? AppTheme.blue.withValues(alpha: .1) : palette.field,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: selected ? AppTheme.blue : Colors.transparent, width: 1.5),
        ),
        child: Column(
          children: [
            Icon(tripTypeIcon(type), color: selected ? AppTheme.blue : palette.muted),
            const SizedBox(height: 4),
            Text(type.shortLabel,
                style: TextStyle(fontWeight: FontWeight.bold, color: selected ? AppTheme.blue : palette.ink)),
          ],
        ),
      ),
    );
  }
}

/// One trip: status, times, students by status, and the actions (start / finish / cancel)
class TripDetailsScreen extends StatefulWidget {
  const TripDetailsScreen({
    super.key,
    required this.tripId,
    this.loadTrip,
    this.startTrip,
    this.completeTrip,
    this.cancelTrip,
    this.loadStudents,
    this.markStudent,
    this.callPhone,
    this.tracker,
    this.loadSchool,
    this.showMapTiles = true,
  });

  final String tripId;

  /// Shares the bus's position while the trip runs (tests pass their own)
  final LocationTracker? tracker;
  final Future<SchoolPlace?> Function()? loadSchool;
  final bool showMapTiles;
  final Future<List<TripRider>> Function(String tripId)? loadStudents;
  final Future<void> Function(String tripId, String studentId, TripStudentStatus status)? markStudent;

  /// Opens the phone dialer (tests pass their own)
  final Future<void> Function(String phone)? callPhone;

  /// Default to the API (tests pass their own)
  final Future<Trip> Function(String id)? loadTrip;
  final Future<void> Function(String id)? startTrip;
  final Future<void> Function(String id)? completeTrip;
  final Future<void> Function(String id)? cancelTrip;

  @override
  State<TripDetailsScreen> createState() => _TripDetailsScreenState();
}

class _TripDetailsScreenState extends State<TripDetailsScreen> with LiveRefreshMixin {
  /// Records saved elsewhere show without refreshing (push, back to the app, every 30 s)
  @override
  Future<void> onLiveRefresh() => _load();

  Trip? _trip;
  String? _error;
  bool _busy = false;

  List<TripRider>? _students;
  String? _studentsError;

  /// Student whose status is being sent
  String? _markingId;

  /// Something changed: the caller reloads its list
  bool _changed = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    _loadStudents();
    try {
      final trip = await (widget.loadTrip ?? TripService.instance.getTrip)(widget.tripId);
      if (mounted) {
        setState(() {
          _trip = trip;
          _error = null;
        });
      }
      _followTracking(trip);
    } catch (e) {
      if (mounted) setState(() => _error = '$e');
    }
  }

  LocationTracker get _tracker => widget.tracker ?? LocationTracker.instance;

  /// On the road: share the position; finished / cancelled: stop sharing it
  void _followTracking(Trip trip) {
    if (trip.status == TripStatus.inProgress) {
      _tracker.start(trip.id);
    } else if (_tracker.tripId == trip.id) {
      _tracker.stop();
    }
  }

  void _openMap() {
    final trip = _trip;
    if (trip == null) return;
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => TripMapScreen(
        trip: trip,
        riders: _students ?? const [],
        loadSchool: widget.loadSchool,
        tracker: widget.tracker,
        showTiles: widget.showMapTiles,
      ),
    ));
  }

  Future<void> _loadStudents() async {
    try {
      final students = await (widget.loadStudents ?? TripService.instance.getTripStudents)(widget.tripId);
      if (mounted) {
        setState(() {
          _students = students;
          _studentsError = null;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _studentsError = '$e');
    }
  }

  /// Boarded / arrived / absent: shown right away, then saved
  Future<void> _mark(TripRider rider, TripStudentStatus status) async {
    setState(() => _markingId = rider.studentId);
    try {
      await (widget.markStudent ?? TripService.instance.markStudent)(widget.tripId, rider.studentId, status);
      _changed = true;
      final now = DateTime.now();
      if (mounted) {
        setState(() {
          _students = [
            for (final s in _students ?? const <TripRider>[])
              s.studentId == rider.studentId
                  ? s.copyWith(
                      status: status,
                      boardedAt: status == TripStudentStatus.boarded ? now : null,
                      droppedOffAt: status == TripStudentStatus.droppedOff ? now : null)
                  : s,
          ];
        });
      }
      final trip = _trip;
      _snack(switch (status) {
        TripStudentStatus.boarded => tr('${rider.name} في الحافلة.', '${rider.name} is on the bus.'),
        TripStudentStatus.droppedOff => trip?.type == TripType.toHome
            ? tr('وصل ${rider.name} إلى المنزل.', '${rider.name} arrived home.')
            : tr('وصل ${rider.name} إلى المدرسة.', '${rider.name} arrived at school.'),
        TripStudentStatus.absent => tr('سُجّل ${rider.name} غائباً.', '${rider.name} was marked absent.'),
        _ => '',
      });
      // Counts on the card follow
      final fresh = await (widget.loadTrip ?? TripService.instance.getTrip)(widget.tripId).catchError((_) => trip!);
      if (mounted) setState(() => _trip = fresh);
    } catch (e) {
      _snack('$e');
    } finally {
      if (mounted) setState(() => _markingId = null);
    }
  }

  Future<void> _call(String phone) async {
    if (widget.callPhone != null) return widget.callPhone!(phone);
    final ok = await launchUrl(Uri(scheme: 'tel', path: phone));
    if (!ok) _snack(tr('تعذر فتح الاتصال.', 'Could not open the dialer.'));
  }

  Future<void> _run(Future<void> Function(String id) action, String done, {String? confirm}) async {
    if (confirm != null && !await _confirm(confirm)) return;
    setState(() => _busy = true);
    try {
      await action(widget.tripId);
      _changed = true;
      await _load();
      _snack(done);
    } catch (e) {
      _snack('$e');
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<bool> _confirm(String text) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (dialog) => AlertDialog(
        content: Text(text),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dialog, false), child: Text(tr('تراجع', 'Back'))),
          FilledButton(onPressed: () => Navigator.pop(dialog, true), child: Text(tr('تأكيد', 'Confirm'))),
        ],
      ),
    );
    return ok == true;
  }

  void _snack(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    final trip = _trip;
    final palette = context.palette;
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.of(context).pop(_changed);
      },
      child: Scaffold(
        appBar: AppBar(
          title: Text(tr('تفاصيل الرحلة', 'Trip details')),
          actions: [
            if (trip != null)
              IconButton(
                key: const ValueKey('open-map'),
                tooltip: tr('خريطة الرحلة', 'Trip map'),
                onPressed: _openMap,
                icon: const Icon(Icons.map_rounded),
              ),
          ],
        ),
        body: trip == null
            ? Center(
                child: _error == null
                    ? const CircularProgressIndicator()
                    : Padding(
                        padding: const EdgeInsets.all(24),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(_error!, textAlign: TextAlign.center),
                            const SizedBox(height: 12),
                            OutlinedButton(onPressed: _load, child: Text(tr('إعادة المحاولة', 'Retry'))),
                          ],
                        ),
                      ),
              )
            : RefreshIndicator(
                onRefresh: _load,
                child: ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    TripCard(
                      trip: trip,
                      showDate: true,
                      busy: _busy,
                      onStart: trip.canStart
                          ? () => _run(widget.startTrip ?? TripService.instance.start, tr('بدأت الرحلة.', 'The trip has started.'))
                          : null,
                      onComplete: trip.canComplete
                          ? () => _run(widget.completeTrip ?? TripService.instance.complete,
                              tr('انتهت الرحلة.', 'The trip is finished.'),
                              confirm: tr('هل وصلت؟ سيتم إنهاء الرحلة.', 'Have you arrived? The trip will be finished.'))
                          : null,
                    ),
                    OutlinedButton.icon(
                      key: const ValueKey('open-map-button'),
                      onPressed: _openMap,
                      style: OutlinedButton.styleFrom(
                        minimumSize: const Size.fromHeight(46),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                      ),
                      icon: const Icon(Icons.map_rounded),
                      label: Text(tr('الخريطة: المدرسة ومنازل الطلاب', "Map: the school and the students' homes")),
                    ),
                    const SizedBox(height: 16),
                    Text(tr('الطلاب', 'Students'),
                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: palette.ink)),
                    const SizedBox(height: 8),
                    GridView.count(
                      crossAxisCount: 2,
                      shrinkWrap: true,
                      physics: const NeverScrollableScrollPhysics(),
                      mainAxisSpacing: 10,
                      crossAxisSpacing: 10,
                      childAspectRatio: 2.3,
                      children: [
                        _count(context, Icons.hourglass_empty_rounded, tr('بانتظار الركوب', 'Waiting'),
                            trip.count(TripStudentStatus.pending), const Color(0xFFE19A00)),
                        _count(context, Icons.directions_bus_filled_rounded, tr('في الحافلة', 'On the bus'),
                            trip.count(TripStudentStatus.boarded), AppTheme.blue),
                        _count(context, Icons.where_to_vote_rounded, tr('تم توصيلهم', 'Dropped off'),
                            trip.count(TripStudentStatus.droppedOff), const Color(0xFF1F9D57)),
                        _count(context, Icons.person_off_outlined, tr('غائبون', 'Absent'),
                            trip.count(TripStudentStatus.absent), const Color(0xFFC0392B)),
                      ],
                    ),
                    const SizedBox(height: 18),
                    ..._studentList(trip),
                    if (trip.canCancel) ...[
                      const SizedBox(height: 20),
                      OutlinedButton.icon(
                        key: const ValueKey('cancel-trip'),
                        onPressed: _busy
                            ? null
                            : () => _run(widget.cancelTrip ?? TripService.instance.cancel, tr('أُلغيت الرحلة.', 'The trip was cancelled.'),
                                confirm: tr('هل تريد إلغاء هذه الرحلة؟', 'Do you want to cancel this trip?')),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: const Color(0xFFC0392B),
                          minimumSize: const Size.fromHeight(46),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        ),
                        icon: const Icon(Icons.cancel_outlined),
                        label: Text(tr('إلغاء الرحلة', 'Cancel trip')),
                      ),
                    ],
                  ],
                ),
              ),
      ),
    );
  }

  List<Widget> _studentList(Trip trip) {
    final palette = context.palette;
    final students = _students;
    final header = Row(
      children: [
        Expanded(
          child: Text(tr('قائمة الطلاب', 'Student list'),
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: palette.ink)),
        ),
        if (trip.status == TripStatus.planned)
          Text(tr('ابدأ الرحلة لتسجيلهم', 'Start the trip to mark them'), style: TextStyle(color: palette.muted, fontSize: 12)),
      ],
    );
    if (students == null) {
      return [
        header,
        const SizedBox(height: 10),
        if (_studentsError != null)
          Column(
            children: [
              Text(_studentsError!, textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
              TextButton(onPressed: _loadStudents, child: Text(tr('إعادة المحاولة', 'Retry'))),
            ],
          )
        else
          const Center(child: Padding(padding: EdgeInsets.all(16), child: CircularProgressIndicator())),
      ];
    }
    if (students.isEmpty) {
      return [
        header,
        const SizedBox(height: 10),
        Text(tr('لا يوجد طلاب في هذه الرحلة.', 'There are no students on this trip.'), style: TextStyle(color: palette.muted)),
      ];
    }
    return [
      header,
      const SizedBox(height: 10),
      for (final s in students)
        _RiderTile(
          key: ValueKey('rider-${s.studentId}'),
          rider: s,
          tripType: trip.type,
          running: trip.status == TripStatus.inProgress,
          busy: _markingId == s.studentId,
          onMark: (status) => _mark(s, status),
          onCall: s.parentPhone.isEmpty ? null : () => _call(s.parentPhone),
        ),
    ];
  }

  Widget _count(BuildContext context, IconData icon, String label, int value, Color color) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(16), border: Border.all(color: palette.line)),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(11)),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('$value', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18, color: palette.ink)),
                Text(label, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontSize: 11.5, color: palette.muted)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Colour and label of a student's status on a trip
(Color, String) riderStatusStyle(TripStudentStatus status, TripType type) => switch (status) {
      TripStudentStatus.pending => (const Color(0xFFE19A00), tr('بانتظار الركوب', 'Waiting')),
      TripStudentStatus.boarded => (AppTheme.blue, tr('في الحافلة', 'On the bus')),
      TripStudentStatus.droppedOff => (
          const Color(0xFF1F9D57),
          type == TripType.toHome ? tr('وصل المنزل', 'Arrived home') : tr('وصل المدرسة', 'Arrived at school')
        ),
      TripStudentStatus.absent => (const Color(0xFFC0392B), tr('غائب', 'Absent')),
    };

/// One student on the trip: name, class, stop, status, and the next step (boarded → arrived, or absent)
class _RiderTile extends StatelessWidget {
  const _RiderTile({
    super.key,
    required this.rider,
    required this.tripType,
    required this.running,
    required this.busy,
    required this.onMark,
    this.onCall,
  });

  final TripRider rider;
  final TripType tripType;

  /// The trip is on the road: the student can be marked
  final bool running;
  final bool busy;
  final void Function(TripStudentStatus status) onMark;
  final VoidCallback? onCall;

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final (color, label) = riderStatusStyle(rider.status, tripType);
    final time = rider.status == TripStudentStatus.droppedOff
        ? rider.droppedOffAt
        : rider.status == TripStudentStatus.boarded
            ? rider.boardedAt
            : null;
    final arrived = tripType == TripType.toHome ? tr('وصل المنزل', 'Arrived home') : tr('وصل المدرسة', 'Arrived at school');

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: palette.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: rider.status == TripStudentStatus.boarded ? color.withValues(alpha: .45) : palette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: (rider.isFemale ? const Color(0xFFDB2777) : AppTheme.blue).withValues(alpha: .12),
                child: Icon(rider.isFemale ? Icons.face_3_rounded : Icons.face_rounded,
                    color: rider.isFemale ? const Color(0xFFDB2777) : AppTheme.blue),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(rider.name,
                        maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                    Text(
                      [rider.classLabel, if (rider.stopName.isNotEmpty) rider.stopName].where((s) => s.isNotEmpty).join(' · '),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 12, color: palette.muted),
                    ),
                  ],
                ),
              ),
              if (onCall != null)
                IconButton(
                  tooltip: tr('الاتصال بولي الأمر', 'Call the parent'),
                  onPressed: onCall,
                  icon: const Icon(Icons.call_rounded, color: Color(0xFF1F9D57)),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              // The status shrinks (ellipsis) so the buttons always fit on a phone
              Expanded(
                child: Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                  child: Text(time == null ? label : '$label · ${formatTime(time)}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(color: color, fontWeight: FontWeight.w700, fontSize: 12)),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              if (busy)
                const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2))
              else if (running && rider.status == TripStudentStatus.pending) ...[
                TextButton(
                  key: ValueKey('absent-${rider.studentId}'),
                  onPressed: () => onMark(TripStudentStatus.absent),
                  style: TextButton.styleFrom(foregroundColor: const Color(0xFFC0392B)),
                  child: Text(tr('غائب', 'Absent')),
                ),
                const SizedBox(width: 4),
                FilledButton.icon(
                  key: ValueKey('board-${rider.studentId}'),
                  onPressed: () => onMark(TripStudentStatus.boarded),
                  style: FilledButton.styleFrom(backgroundColor: AppTheme.blue, visualDensity: VisualDensity.compact),
                  icon: const Icon(Icons.directions_bus_filled_rounded, size: 18),
                  label: Text(tr('ركب', 'Boarded')),
                ),
              ] else if (running && rider.status == TripStudentStatus.boarded)
                FilledButton.icon(
                  key: ValueKey('arrive-${rider.studentId}'),
                  onPressed: () => onMark(TripStudentStatus.droppedOff),
                  style: FilledButton.styleFrom(backgroundColor: const Color(0xFF1F9D57), visualDensity: VisualDensity.compact),
                  icon: Icon(tripType == TripType.toHome ? Icons.home_rounded : Icons.school_rounded, size: 18),
                  label: Text(arrived),
                ),
            ],
          ),
        ],
      ),
    );
  }
}
