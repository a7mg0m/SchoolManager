import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/theme/app_theme.dart';
import 'school_info_service.dart';

/// About the school: logo and name, contact details and the school album.
class SchoolInfoScreen extends StatefulWidget {
  const SchoolInfoScreen({super.key, this.loadSchoolInfo});

  /// Defaults to the API (tests pass their own)
  final Future<SchoolInfo?> Function()? loadSchoolInfo;

  @override
  State<SchoolInfoScreen> createState() => _SchoolInfoScreenState();
}

class _SchoolInfoScreenState extends State<SchoolInfoScreen> {
  static const _muted = Color(0xFF637394);
  static const _red = Color(0xFFD93025);

  SchoolInfo? _info;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _info == null;
      _error = null;
    });
    try {
      final loader = widget.loadSchoolInfo ?? SchoolInfoService.instance.getSchoolInfo;
      final result = await loader();
      if (!mounted) return;
      setState(() {
        _info = result;
        _loading = false;
      });
    } on SchoolInfoException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('عن المدرسة', style: TextStyle(fontWeight: FontWeight.bold))),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _centerMessage(Icons.error_outline_rounded, _error!, retry: true)
                : _info == null
                    ? _centerMessage(Icons.school_outlined, 'لم تُضف بيانات المدرسة بعد.')
                    : RefreshIndicator(onRefresh: _load, child: _content(_info!)),
      ),
    );
  }

  Widget _content(SchoolInfo info) {
    final contacts = [
      if (info.address != null) (Icons.location_on_rounded, 'العنوان', info.address!),
      if (info.phone != null) (Icons.phone_rounded, 'الهاتف', info.phone!),
      if (info.email != null) (Icons.email_rounded, 'البريد الإلكتروني', info.email!),
    ];

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _header(info),
        if (contacts.isNotEmpty) ...[
          const SizedBox(height: 18),
          _sectionTitle('بيانات التواصل', Icons.contact_phone_rounded),
          Container(
            decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(18)),
            child: Column(
              children: [
                for (var i = 0; i < contacts.length; i++) ...[
                  if (i > 0) const Divider(height: 1, indent: 60, color: Color(0xFFEDF1F7)),
                  _contactRow(contacts[i].$1, contacts[i].$2, contacts[i].$3),
                ],
              ],
            ),
          ),
        ],
        const SizedBox(height: 22),
        _sectionTitle('ألبوم المدرسة', Icons.photo_library_rounded, count: info.album.length),
        if (info.album.isEmpty)
          Container(
            padding: const EdgeInsets.symmetric(vertical: 30),
            decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(18)),
            child: const Column(
              children: [
                Icon(Icons.photo_outlined, size: 46, color: Color(0xFFA0AABF)),
                SizedBox(height: 8),
                Text('لا توجد صور في الألبوم بعد.', style: TextStyle(color: _muted)),
              ],
            ),
          )
        else
          _albumSlider(info.album),
      ],
    );
  }

  /// Logo and name on the app's blue gradient
  Widget _header(SchoolInfo info) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 24, 20, 22),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF1677D2), Color(0xFF06468F)],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(24),
      ),
      child: Column(
        children: [
          Container(
            width: 104,
            height: 104,
            padding: const EdgeInsets.all(10),
            decoration: const BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
              boxShadow: [BoxShadow(color: Color(0x33000000), blurRadius: 16, offset: Offset(0, 6))],
            ),
            child: ClipOval(
              child: info.logo != null
                  ? Image.memory(info.logo!, fit: BoxFit.contain, errorBuilder: (_, __, ___) => _defaultLogo())
                  : _defaultLogo(),
            ),
          ),
          const SizedBox(height: 14),
          Text(info.name,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.white, fontSize: 21, fontWeight: FontWeight.bold)),
          if (info.nameEn.isNotEmpty && info.nameEn != info.name) ...[
            const SizedBox(height: 4),
            Text(info.nameEn,
                textAlign: TextAlign.center,
                textDirection: TextDirection.ltr,
                style: const TextStyle(color: Colors.white70, fontSize: 14)),
          ],
        ],
      ),
    );
  }

  Widget _defaultLogo() => Image.asset('assets/images/logo.png', fit: BoxFit.contain);

  /// One contact detail; tap to copy it
  Widget _contactRow(IconData icon, String label, String value) {
    return InkWell(
      borderRadius: BorderRadius.circular(18),
      onTap: () async {
        await Clipboard.setData(ClipboardData(text: value));
        if (!mounted) return;
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(SnackBar(content: Text('تم نسخ $label')));
      },
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        child: Row(
          children: [
            CircleAvatar(
              radius: 20,
              backgroundColor: AppTheme.blue.withValues(alpha: .10),
              child: Icon(icon, color: AppTheme.blue, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: const TextStyle(fontSize: 11.5, color: _muted)),
                  const SizedBox(height: 2),
                  Text(value, style: const TextStyle(fontSize: 14.5, fontWeight: FontWeight.w600, color: AppTheme.ink)),
                ],
              ),
            ),
            const Icon(Icons.copy_rounded, size: 18, color: Color(0xFFA0AABF)),
          ],
        ),
      ),
    );
  }

  /// Album as a slider (auto-plays); tap a photo to open the viewer
  Widget _albumSlider(List<AlbumImage> album) {
    return AlbumSlider(
      images: album,
      onOpen: (i) => Navigator.of(context).push(MaterialPageRoute(
        builder: (_) => AlbumViewerScreen(images: album, initialIndex: i),
      )),
    );
  }

  Widget _sectionTitle(String title, IconData icon, {int? count}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          Icon(icon, size: 20, color: AppTheme.blue),
          const SizedBox(width: 6),
          Text(title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.ink)),
          if (count != null) ...[
            const SizedBox(width: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
              decoration: BoxDecoration(color: const Color(0xFFE3E9F3), borderRadius: BorderRadius.circular(10)),
              child: Text('$count', style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: _muted)),
            ),
          ],
        ],
      ),
    );
  }

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

/// Network image with a loading and a "broken image" placeholder
Widget _networkImage(String url, BoxFit fit) {
  return Image.network(
    url,
    fit: fit,
    loadingBuilder: (context, child, progress) => progress == null
        ? child
        : const ColoredBox(
            color: Color(0xFFE9EEF6),
            child: Center(child: SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2))),
          ),
    errorBuilder: (_, __, ___) => const ColoredBox(
      color: Color(0xFFE9EEF6),
      child: Center(child: Icon(Icons.broken_image_outlined, color: Color(0xFFA0AABF))),
    ),
  );
}

/// Photo slider: large rounded photos with the next one peeking in, dots and a counter.
/// Moves on by itself every [interval]; touching it pauses the auto-play until the swipe ends.
class AlbumSlider extends StatefulWidget {
  const AlbumSlider({
    super.key,
    required this.images,
    required this.onOpen,
    this.interval = const Duration(seconds: 4),
  });

  final List<AlbumImage> images;

  /// Tap on a photo (index in [images])
  final void Function(int index) onOpen;
  final Duration interval;

  @override
  State<AlbumSlider> createState() => _AlbumSliderState();
}

class _AlbumSliderState extends State<AlbumSlider> {
  final _controller = PageController(viewportFraction: .88);
  int _index = 0;
  Timer? _timer;
  bool _userDragging = false;

  @override
  void initState() {
    super.initState();
    _startAutoPlay();
  }

  @override
  void didUpdateWidget(AlbumSlider oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (_index >= widget.images.length) _index = 0;
    _startAutoPlay();
  }

  @override
  void dispose() {
    _timer?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _startAutoPlay() {
    _timer?.cancel();
    if (widget.images.length < 2) return;
    _timer = Timer.periodic(widget.interval, (_) {
      if (!mounted || _userDragging || !_controller.hasClients) return;
      final next = (_index + 1) % widget.images.length;
      _controller.animateToPage(next, duration: const Duration(milliseconds: 550), curve: Curves.easeInOutCubic);
    });
  }

  @override
  Widget build(BuildContext context) {
    final images = widget.images;
    return Column(
      children: [
        AspectRatio(
          aspectRatio: 16 / 10,
          child: NotificationListener<ScrollNotification>(
            // Pause the auto-play while the user swipes
            onNotification: (n) {
              if (n is ScrollStartNotification && n.dragDetails != null) _userDragging = true;
              if (n is ScrollEndNotification && _userDragging) {
                _userDragging = false;
                _startAutoPlay();
              }
              return false;
            },
            child: PageView.builder(
              controller: _controller,
              itemCount: images.length,
              onPageChanged: (i) => setState(() => _index = i),
              itemBuilder: (context, i) => AnimatedScale(
                // The current photo is full size, the neighbours slightly smaller
                scale: i == _index ? 1 : .94,
                duration: const Duration(milliseconds: 300),
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 6),
                  child: GestureDetector(
                    key: ValueKey('album-slide-$i'),
                    onTap: () => widget.onOpen(i),
                    child: Hero(
                      tag: 'album-${images[i].id}',
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(20),
                        child: Stack(
                          fit: StackFit.expand,
                          children: [
                            _networkImage(images[i].url, BoxFit.cover),
                            // Counter on the photo
                            Positioned(
                              top: 10,
                              left: 10,
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                                decoration: BoxDecoration(color: Colors.black45, borderRadius: BorderRadius.circular(20)),
                                child: Text('${i + 1} من ${images.length}',
                                    style: const TextStyle(color: Colors.white, fontSize: 11.5, fontWeight: FontWeight.bold)),
                              ),
                            ),
                            const Positioned(
                              bottom: 10,
                              right: 10,
                              child: Icon(Icons.zoom_out_map_rounded, color: Colors.white70, size: 20),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
        if (images.length > 1) ...[
          const SizedBox(height: 10),
          // Dots (tap one to jump to that photo)
          Wrap(
            alignment: WrapAlignment.center,
            spacing: 6,
            runSpacing: 6,
            children: [
              for (var i = 0; i < images.length; i++)
                GestureDetector(
                  onTap: () => _controller.animateToPage(i,
                      duration: const Duration(milliseconds: 400), curve: Curves.easeOutCubic),
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 250),
                    width: i == _index ? 22 : 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: i == _index ? AppTheme.blue : const Color(0xFFD3DDEE),
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                ),
            ],
          ),
        ],
      ],
    );
  }
}

/// Full-screen album: swipe between photos, pinch to zoom
class AlbumViewerScreen extends StatefulWidget {
  const AlbumViewerScreen({super.key, required this.images, this.initialIndex = 0});

  final List<AlbumImage> images;
  final int initialIndex;

  @override
  State<AlbumViewerScreen> createState() => _AlbumViewerScreenState();
}

class _AlbumViewerScreenState extends State<AlbumViewerScreen> {
  late final PageController _controller = PageController(initialPage: widget.initialIndex);
  late int _index = widget.initialIndex;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        title: Text('${_index + 1} / ${widget.images.length}', style: const TextStyle(fontSize: 16)),
      ),
      body: PageView.builder(
        controller: _controller,
        itemCount: widget.images.length,
        onPageChanged: (i) => setState(() => _index = i),
        itemBuilder: (context, i) => InteractiveViewer(
          minScale: 1,
          maxScale: 4,
          child: Center(
            child: Hero(
              tag: 'album-${widget.images[i].id}',
              child: _networkImage(widget.images[i].url, BoxFit.contain),
            ),
          ),
        ),
      ),
    );
  }
}
