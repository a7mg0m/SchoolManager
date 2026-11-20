import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/app_theme.dart';
import '../../core/i18n/app_language.dart';

/// Google Maps links for a point (open the maps app on the phone, the website on the web)
class MapLinks {
  static Uri view(double lat, double lng) =>
      Uri.parse('https://www.google.com/maps/search/?api=1&query=$lat,$lng');

  static Uri directions(double lat, double lng) =>
      Uri.parse('https://www.google.com/maps/dir/?api=1&destination=$lat,$lng');

  /// Opens [uri]; shows a message when nothing can open it
  static Future<void> open(BuildContext context, Uri uri) async {
    final messenger = ScaffoldMessenger.of(context);
    var opened = false;
    try {
      opened = await launchUrl(uri, mode: LaunchMode.externalApplication);
    } catch (_) {
      opened = false;
    }
    if (!opened) {
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(tr('تعذر فتح تطبيق الخرائط.', 'Could not open the maps app.'))));
    }
  }
}

/// OpenStreetMap map with the school pin.
/// [interactive] false: a still preview (it sits inside a scrolling page).
/// [showTiles] false skips the map images (widget tests have no network).
class SchoolMap extends StatelessWidget {
  const SchoolMap({
    super.key,
    required this.latitude,
    required this.longitude,
    this.schoolName,
    this.interactive = false,
    this.showTiles = true,
    this.mapController,
  });

  final double latitude;
  final double longitude;
  final String? schoolName;
  final bool interactive;
  final bool showTiles;
  final MapController? mapController;

  @override
  Widget build(BuildContext context) {
    final point = LatLng(latitude, longitude);
    return FlutterMap(
      mapController: mapController,
      options: MapOptions(
        initialCenter: point,
        initialZoom: interactive ? 16 : 15,
        minZoom: 3,
        maxZoom: 19,
        backgroundColor: context.palette.placeholder,
        interactionOptions: InteractionOptions(
          flags: interactive ? InteractiveFlag.all & ~InteractiveFlag.rotate : InteractiveFlag.none,
        ),
      ),
      children: [
        if (showTiles)
          TileLayer(
            urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
            userAgentPackageName: 'com.schoolmanager.smart_school_teacher',
            maxZoom: 19,
          ),
        MarkerLayer(
          markers: [
            Marker(
              point: point,
              width: 46,
              height: 46,
              alignment: Alignment.topCenter,
              child: const Icon(Icons.location_on_rounded,
                  key: ValueKey('school-pin'),
                  size: 46,
                  color: Color(0xFFD93025),
                  shadows: [Shadow(color: Color(0x55000000), blurRadius: 6, offset: Offset(0, 2))]),
            ),
          ],
        ),
        if (showTiles)
          const SimpleAttributionWidget(source: Text('OpenStreetMap contributors')),
      ],
    );
  }
}

/// "School location" card on the About-the-school page: map preview, coordinates and buttons.
class SchoolLocationCard extends StatelessWidget {
  const SchoolLocationCard({
    super.key,
    required this.latitude,
    required this.longitude,
    this.schoolName,
    this.address,
    this.showTiles = true,
  });

  final double latitude;
  final double longitude;
  final String? schoolName;
  final String? address;
  final bool showTiles;

  void _openFullScreen(BuildContext context) {
    Navigator.of(context).push(MaterialPageRoute(
      builder: (_) => SchoolMapScreen(
        latitude: latitude,
        longitude: longitude,
        schoolName: schoolName,
        address: address,
        showTiles: showTiles,
      ),
    ));
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(color: context.palette.card, borderRadius: BorderRadius.circular(18)),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AspectRatio(
            aspectRatio: 16 / 9,
            child: Stack(
              fit: StackFit.expand,
              children: [
                SchoolMap(latitude: latitude, longitude: longitude, showTiles: showTiles),
                // The preview does not move: a tap opens the full map
                Positioned.fill(
                  child: Material(
                    color: Colors.transparent,
                    child: InkWell(
                      key: const ValueKey('school-map-preview'),
                      onTap: () => _openFullScreen(context),
                    ),
                  ),
                ),
                Positioned(
                  top: 10,
                  left: 10,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                    decoration: BoxDecoration(color: Colors.black54, borderRadius: BorderRadius.circular(20)),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.fullscreen_rounded, color: Colors.white, size: 16),
                        SizedBox(width: 4),
                        Text(tr('عرض الخريطة', 'View map'), style: TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 10, 12, 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                if (address != null) ...[
                  Row(
                    children: [
                      const Icon(Icons.place_outlined, size: 18, color: AppTheme.blue),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Text(address!,
                            style: TextStyle(fontSize: 13.5, fontWeight: FontWeight.w600, color: context.palette.ink)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                ],
                Row(
                  children: [
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: () => MapLinks.open(context, MapLinks.directions(latitude, longitude)),
                        icon: const Icon(Icons.directions_rounded, size: 20),
                        label: Text(tr('الاتجاهات', 'Directions')),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => MapLinks.open(context, MapLinks.view(latitude, longitude)),
                        icon: const Icon(Icons.map_outlined, size: 20),
                        label: Text(tr('فتح في الخرائط', 'Open in Maps')),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Full-screen map: move and zoom, back to the school, directions
class SchoolMapScreen extends StatefulWidget {
  const SchoolMapScreen({
    super.key,
    required this.latitude,
    required this.longitude,
    this.schoolName,
    this.address,
    this.showTiles = true,
  });

  final double latitude;
  final double longitude;
  final String? schoolName;
  final String? address;
  final bool showTiles;

  @override
  State<SchoolMapScreen> createState() => _SchoolMapScreenState();
}

class _SchoolMapScreenState extends State<SchoolMapScreen> {
  final _controller = MapController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final coordinates = '${widget.latitude.toStringAsFixed(6)}, ${widget.longitude.toStringAsFixed(6)}';
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.schoolName ?? tr('موقع المدرسة', 'School location'), style: const TextStyle(fontWeight: FontWeight.bold)),
      ),
      body: Stack(
        children: [
          SchoolMap(
            latitude: widget.latitude,
            longitude: widget.longitude,
            interactive: true,
            showTiles: widget.showTiles,
            mapController: _controller,
          ),
          // Back to the school after moving the map
          PositionedDirectional(
            top: 12,
            end: 12,
            child: FloatingActionButton.small(
              heroTag: 'school-map-center',
              backgroundColor: context.palette.card,
              foregroundColor: AppTheme.blue,
              tooltip: tr('موقع المدرسة', 'School location'),
              onPressed: () => _controller.move(LatLng(widget.latitude, widget.longitude), 16),
              child: const Icon(Icons.my_location_rounded),
            ),
          ),
          Positioned(
            left: 12,
            right: 12,
            bottom: 12,
            child: SafeArea(
              top: false,
              child: Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: context.palette.card,
                  borderRadius: BorderRadius.circular(18),
                  boxShadow: const [BoxShadow(color: Color(0x22000000), blurRadius: 14, offset: Offset(0, 4))],
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (widget.address != null)
                      Text(widget.address!,
                          style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: context.palette.ink)),
                    // Coordinates: tap to copy
                    InkWell(
                      onTap: () async {
                        await Clipboard.setData(ClipboardData(text: coordinates));
                        if (!context.mounted) return;
                        ScaffoldMessenger.of(context)
                          ..hideCurrentSnackBar()
                          ..showSnackBar(SnackBar(content: Text(tr('تم نسخ الإحداثيات', 'Coordinates copied'))));
                      },
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 6),
                        child: Row(
                          children: [
                            Icon(Icons.gps_fixed_rounded, size: 16, color: context.palette.muted),
                            const SizedBox(width: 6),
                            Text(coordinates,
                                textDirection: TextDirection.ltr,
                                style: TextStyle(fontSize: 12.5, color: context.palette.muted)),
                            const SizedBox(width: 6),
                            Icon(Icons.copy_rounded, size: 14, color: context.palette.faint),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 6),
                    FilledButton.icon(
                      onPressed: () => MapLinks.open(context, MapLinks.directions(widget.latitude, widget.longitude)),
                      icon: const Icon(Icons.directions_rounded),
                      label: Text(tr('الاتجاهات إلى المدرسة', 'Directions to the school')),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
