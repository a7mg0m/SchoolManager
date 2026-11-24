import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'location_tracker.dart';
import 'trip_screens.dart' show riderStatusStyle;
import 'trip_service.dart';

/// Map of a trip: the school, each student's home in the order of the stops (to school: homes → school,
/// to home: school → homes), and the bus's position. Straight lines between the points (not the roads).
class TripMapScreen extends StatefulWidget {
  const TripMapScreen({
    super.key,
    required this.trip,
    required this.riders,
    this.loadSchool,
    this.tracker,
    this.showTiles = true,
    this.openDirections,
  });

  final Trip trip;

  /// In the order the bus passes them (from the API)
  final List<TripRider> riders;

  /// Default to the API (tests pass their own)
  final Future<SchoolPlace?> Function()? loadSchool;
  final LocationTracker? tracker;

  /// Tests switch the map tiles off (no network)
  final bool showTiles;
  final Future<void> Function(double latitude, double longitude)? openDirections;

  @override
  State<TripMapScreen> createState() => _TripMapScreenState();
}

class _TripMapScreenState extends State<TripMapScreen> {
  final _map = MapController();
  SchoolPlace? _school;
  bool _loading = true;

  static const _distance = Distance();
  static const _green = Color(0xFF1F9D57);

  LocationTracker get _tracker => widget.tracker ?? LocationTracker.instance;

  @override
  void initState() {
    super.initState();
    _tracker.addListener(_onTracker);
    _loadSchool();
  }

  @override
  void dispose() {
    _tracker.removeListener(_onTracker);
    super.dispose();
  }

  void _onTracker() {
    if (mounted) setState(() {});
  }

  Future<void> _loadSchool() async {
    try {
      final school = await (widget.loadSchool ?? TripService.instance.getSchool)();
      if (mounted) setState(() => _school = school);
    } catch (_) {
      // The students still show without the school
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  LatLng? get _schoolPoint {
    final s = _school;
    return s != null && s.hasLocation ? LatLng(s.latitude!, s.longitude!) : null;
  }

  /// The bus: the phone's position while sharing this trip, else the last one the server has
  LatLng? get _busPoint {
    final p = _tracker.tripId == widget.trip.id ? _tracker.last : null;
    if (p != null) return LatLng(p.$1, p.$2);
    final t = widget.trip;
    return t.currentLatitude != null && t.currentLongitude != null ? LatLng(t.currentLatitude!, t.currentLongitude!) : null;
  }

  List<TripRider> get _located => widget.riders.where((r) => r.hasLocation).toList();

  /// The stops in driving order: the school first on the way home, last on the way to school
  List<_Stop> get _stops {
    final homes = [for (final (i, r) in _located.indexed) _Stop(LatLng(r.latitude!, r.longitude!), rider: r, number: i + 1)];
    final school = _schoolPoint;
    if (school == null) return homes;
    final schoolStop = _Stop(school);
    return widget.trip.type == TripType.toHome ? [schoolStop, ...homes] : [...homes, schoolStop];
  }

  double get _totalKm {
    final stops = _stops;
    var meters = 0.0;
    for (var i = 1; i < stops.length; i++) {
      meters += _distance(stops[i - 1].point, stops[i].point);
    }
    return meters / 1000;
  }

  Future<void> _directions(LatLng point) async {
    if (widget.openDirections != null) return widget.openDirections!(point.latitude, point.longitude);
    final uri = Uri.parse('https://www.google.com/maps/dir/?api=1&destination=${point.latitude},${point.longitude}&travelmode=driving');
    if (!await launchUrl(uri, mode: LaunchMode.externalApplication) && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(tr('تعذر فتح الخرائط.', 'Could not open the maps app.'))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final palette = context.palette;
    final stops = _stops;
    final bus = _busPoint;
    final points = [...stops.map((s) => s.point), ?bus];
    final missing = widget.riders.length - _located.length;

    return Scaffold(
      appBar: AppBar(title: Text(tr('خريطة الرحلة', 'Trip map'))),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                Expanded(
                  child: points.isEmpty
                      ? Center(
                          child: Padding(
                            padding: const EdgeInsets.all(24),
                            child: Text(
                              tr(
                                'لا توجد مواقع لعرضها بعد: لم تُحدَّد مواقع منازل الطلاب ولا موقع المدرسة.',
                                "Nothing to show yet: no student's home location and no school location has been set.",
                              ),
                              textAlign: TextAlign.center,
                              style: TextStyle(color: palette.muted),
                            ),
                          ),
                        )
                      : FlutterMap(
                          mapController: _map,
                          options: MapOptions(
                            initialCenter: points.first,
                            initialZoom: 14,
                            initialCameraFit: points.length > 1
                                ? CameraFit.coordinates(coordinates: points, padding: const EdgeInsets.all(48), maxZoom: 17)
                                : null,
                            minZoom: 3,
                            maxZoom: 19,
                            backgroundColor: palette.placeholder,
                            interactionOptions: const InteractionOptions(flags: InteractiveFlag.all & ~InteractiveFlag.rotate),
                          ),
                          children: [
                            if (widget.showTiles)
                              TileLayer(
                                urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                                userAgentPackageName: 'com.schoolmanager.smart_school_driver',
                                maxZoom: 19,
                              ),
                            if (stops.length > 1)
                              PolylineLayer(
                                polylines: [
                                  Polyline(
                                    points: stops.map((s) => s.point).toList(),
                                    strokeWidth: 4,
                                    color: AppTheme.blue.withValues(alpha: .7),
                                    pattern: StrokePattern.dashed(segments: const [12, 8]),
                                  ),
                                ],
                              ),
                            MarkerLayer(
                              markers: [
                                for (final s in stops)
                                  Marker(
                                    point: s.point,
                                    width: 40,
                                    height: 40,
                                    child: s.rider == null
                                        ? _pin(Icons.school_rounded, AppTheme.navy, 'school-pin')
                                        : _numberPin(s.number!, riderStatusStyle(s.rider!.status, widget.trip.type).$1, s.rider!.studentId),
                                  ),
                                if (bus != null)
                                  Marker(point: bus, width: 46, height: 46, child: _pin(Icons.directions_bus_rounded, _green, 'bus-pin')),
                              ],
                            ),
                            if (widget.showTiles) const SimpleAttributionWidget(source: Text('OpenStreetMap contributors')),
                          ],
                        ),
                ),
                _panel(stops, missing),
              ],
            ),
    );
  }

  Widget _pin(IconData icon, Color color, String key) => Container(
    key: ValueKey(key),
    decoration: BoxDecoration(
      color: color,
      shape: BoxShape.circle,
      border: Border.all(color: Colors.white, width: 3),
      boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 6)],
    ),
    child: Icon(icon, color: Colors.white, size: 20),
  );

  Widget _numberPin(int number, Color color, String studentId) => Container(
    key: ValueKey('student-pin-$studentId'),
    alignment: Alignment.center,
    decoration: BoxDecoration(
      color: color,
      shape: BoxShape.circle,
      border: Border.all(color: Colors.white, width: 3),
      boxShadow: const [BoxShadow(color: Colors.black26, blurRadius: 6)],
    ),
    child: Text(
      '$number',
      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
    ),
  );

  /// The stops in order, with the distance from the previous one; tap to center, arrow for directions
  Widget _panel(List<_Stop> stops, int missing) {
    final palette = context.palette;
    final tracking = _tracker.tripId == widget.trip.id;
    // A Material (not a coloured box) so the rows' tap ripples show
    return Material(
      color: palette.card,
      elevation: 8,
      shadowColor: Colors.black26,
      borderRadius: const BorderRadius.vertical(top: Radius.circular(22)),
      child: ConstrainedBox(
        constraints: BoxConstraints(maxHeight: MediaQuery.of(context).size.height * .42),
        child: SafeArea(
          top: false,
          child: ListView(
            shrinkWrap: true,
            padding: const EdgeInsets.fromLTRB(16, 14, 16, 12),
            children: [
              Row(
                children: [
                  Icon(widget.trip.type == TripType.toHome ? Icons.home_rounded : Icons.school_rounded, color: AppTheme.blue),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      widget.trip.type.label,
                      style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink),
                    ),
                  ),
                  if (stops.length > 1)
                    Text(
                      tr('${_totalKm.toStringAsFixed(1)} كم تقريباً', 'about ${_totalKm.toStringAsFixed(1)} km'),
                      key: const ValueKey('total-km'),
                      style: TextStyle(color: palette.muted, fontWeight: FontWeight.w600),
                    ),
                ],
              ),
              if (tracking) ...[
                const SizedBox(height: 6),
                Row(
                  children: [
                    Icon(
                      _tracker.error == null ? Icons.my_location_rounded : Icons.location_disabled_rounded,
                      size: 16,
                      color: _tracker.error == null ? _green : const Color(0xFFC0392B),
                    ),
                    const SizedBox(width: 6),
                    Expanded(
                      child: Text(
                        _tracker.error ?? tr('يتم مشاركة موقع الحافلة تلقائياً', "The bus's position is shared automatically"),
                        style: TextStyle(fontSize: 12, color: _tracker.error == null ? _green : const Color(0xFFC0392B)),
                      ),
                    ),
                  ],
                ),
              ],
              if (_schoolPoint == null) ...[
                const SizedBox(height: 6),
                Text(
                  tr('لم يُحدَّد موقع المدرسة بعد.', "The school's location has not been set yet."),
                  style: TextStyle(fontSize: 12, color: palette.muted),
                ),
              ],
              if (missing > 0) ...[
                const SizedBox(height: 6),
                Text(
                  tr(
                    '$missing طالب بدون موقع منزل (يحدده ولي الأمر من تطبيقه).',
                    "$missing student(s) have no home location (the parent sets it in the parent app).",
                  ),
                  key: const ValueKey('missing-locations'),
                  style: TextStyle(fontSize: 12, color: palette.muted),
                ),
              ],
              const SizedBox(height: 8),
              for (final (i, s) in stops.indexed)
                ListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  onTap: () => _map.move(s.point, 16),
                  leading: s.rider == null
                      ? const CircleAvatar(
                          radius: 15,
                          backgroundColor: AppTheme.navy,
                          child: Icon(Icons.school_rounded, color: Colors.white, size: 16),
                        )
                      : CircleAvatar(
                          radius: 15,
                          backgroundColor: riderStatusStyle(s.rider!.status, widget.trip.type).$1,
                          child: Text(
                            '${s.number}',
                            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12),
                          ),
                        ),
                  title: Text(
                    s.rider?.name ?? (_school?.name ?? tr('المدرسة', 'School')),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: palette.ink, fontWeight: FontWeight.w600),
                  ),
                  subtitle: Text(
                    [
                      if (s.rider != null) riderStatusStyle(s.rider!.status, widget.trip.type).$2,
                      if (i > 0)
                        tr(
                          '${(_distance(stops[i - 1].point, s.point) / 1000).toStringAsFixed(1)} كم من السابق',
                          '${(_distance(stops[i - 1].point, s.point) / 1000).toStringAsFixed(1)} km from the previous',
                        ),
                    ].join(' · '),
                    style: TextStyle(fontSize: 12, color: palette.muted),
                  ),
                  trailing: IconButton(
                    tooltip: tr('الاتجاهات', 'Directions'),
                    onPressed: () => _directions(s.point),
                    icon: const Icon(Icons.directions_rounded, color: AppTheme.blue),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Stop {
  const _Stop(this.point, {this.rider, this.number});
  final LatLng point;

  /// Null for the school
  final TripRider? rider;
  final int? number;
}
