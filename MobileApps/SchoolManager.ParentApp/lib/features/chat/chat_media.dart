import 'dart:async';

import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:just_audio/just_audio.dart';
import 'package:path_provider/path_provider.dart';
import 'package:record/record.dart';
import 'package:video_player/video_player.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import 'chat_service.dart';

/// Pictures, videos and voice notes of the chat: what a bubble shows, the full-screen viewers,
/// picking from the camera / gallery and recording a voice note.

/// "1:05"
String chatDuration(Duration d) {
  final m = d.inMinutes;
  final s = d.inSeconds.remainder(60).toString().padLeft(2, '0');
  return '$m:$s';
}

/// Picks a picture / video with the camera or from the gallery; null when cancelled.
/// [onError] gets a message ready to show (too large, could not read).
Future<ChatOutgoingMedia?> pickChatMedia({required bool video, required ImageSource source, required void Function(String) onError}) async {
  final picker = ImagePicker();
  final XFile? file;
  try {
    file = video
        ? await picker.pickVideo(source: source, maxDuration: const Duration(minutes: 3))
        : await picker.pickImage(source: source, imageQuality: 80, maxWidth: 1920, maxHeight: 1920);
  } catch (_) {
    onError(tr('تعذر فتح الكاميرا أو المعرض.', 'Could not open the camera or the gallery.'));
    return null;
  }
  if (file == null) return null;

  final bytes = await file.readAsBytes();
  final ext = _extensionOf(file.name).isNotEmpty ? _extensionOf(file.name) : _extensionOf(file.path);
  final name = '${video ? 'video' : 'photo'}_${DateTime.now().millisecondsSinceEpoch}.${ext.isNotEmpty ? ext : (video ? 'mp4' : 'jpg')}';
  final media = ChatOutgoingMedia(bytes: bytes, fileName: name, kind: video ? ChatMediaKind.video : ChatMediaKind.image);
  if (media.tooLarge) {
    onError(video
        ? tr('الفيديو كبير جداً (الحد 25 ميجابايت). اختر فيديو أقصر.', 'The video is too large (25 MB max). Choose a shorter one.')
        : tr('الصورة كبيرة جداً (الحد 10 ميجابايت).', 'The picture is too large (10 MB max).'));
    return null;
  }
  return media;
}

String _extensionOf(String name) {
  final i = name.lastIndexOf('.');
  if (i < 0 || i == name.length - 1) return '';
  final ext = name.substring(i + 1).toLowerCase();
  return ext.length <= 5 ? ext : '';
}

/// Records a voice note: [start] (asks for the microphone), then [stop] → the note, or [cancel].
/// The plugin is only created on the first recording (screens without one never touch it).
class ChatVoiceRecorder {
  AudioRecorder? _recorder;
  DateTime? _startedAt;
  String _fileName = 'voice.m4a';

  /// Longest voice note
  static const maxLength = Duration(minutes: 5);

  bool get isRecording => _startedAt != null;
  Duration get elapsed => _startedAt == null ? Duration.zero : DateTime.now().difference(_startedAt!);

  /// False when the microphone is not allowed (or not available)
  Future<bool> start() async {
    if (isRecording) return true;
    final recorder = _recorder ??= AudioRecorder();
    try {
      if (!await recorder.hasPermission()) return false;
      // AAC (.m4a) on phones; browsers record Opus in WebM
      final aac = !kIsWeb && await recorder.isEncoderSupported(AudioEncoder.aacLc);
      final encoder = aac ? AudioEncoder.aacLc : AudioEncoder.opus;
      final stamp = DateTime.now().millisecondsSinceEpoch;
      _fileName = 'voice_$stamp.${aac ? 'm4a' : (kIsWeb ? 'weba' : 'ogg')}';
      final path = kIsWeb ? '' : '${(await getTemporaryDirectory()).path}/$_fileName';
      await recorder.start(RecordConfig(encoder: encoder, bitRate: 64000, sampleRate: 44100, numChannels: 1), path: path);
      _startedAt = DateTime.now();
      return true;
    } catch (_) {
      _startedAt = null;
      return false;
    }
  }

  /// The recorded note (null when too short / it failed)
  Future<ChatOutgoingMedia?> stop() async {
    final recorder = _recorder;
    final length = elapsed;
    _startedAt = null;
    if (recorder == null) return null;
    try {
      final path = await recorder.stop();
      if (path == null || path.isEmpty || length < const Duration(milliseconds: 800)) return null;
      // A file on phones, a blob: URL in browsers: XFile reads both
      final bytes = await XFile(path).readAsBytes();
      if (bytes.isEmpty) return null;
      return ChatOutgoingMedia(bytes: bytes, fileName: _fileName, kind: ChatMediaKind.audio, duration: length);
    } catch (_) {
      return null;
    }
  }

  Future<void> cancel() async {
    _startedAt = null;
    try {
      await _recorder?.cancel();
    } catch (_) {}
  }

  Future<void> dispose() async {
    _startedAt = null;
    try {
      await _recorder?.dispose();
    } catch (_) {}
    _recorder = null;
  }
}

/// What a message carries, inside its bubble: a picture, a video or a voice note.
/// [local] is a file still being sent from this phone (shown from memory).
class ChatMediaView extends StatelessWidget {
  const ChatMediaView({super.key, this.attachment, this.local, required this.mine, this.sending = false});

  final ChatAttachment? attachment;
  final ChatOutgoingMedia? local;
  final bool mine;
  final bool sending;

  ChatMediaKind get _kind => attachment?.kind ?? local?.kind ?? ChatMediaKind.file;

  @override
  Widget build(BuildContext context) {
    switch (_kind) {
      case ChatMediaKind.image:
        return _image(context);
      case ChatMediaKind.video:
        return _video(context);
      case ChatMediaKind.audio:
        return attachment != null
            ? ChatVoicePlayer(key: ValueKey(attachment!.url), url: ChatService.instance.fileUrl(attachment!), mine: mine)
            : _voiceSending(context);
      case ChatMediaKind.file:
        return Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(Icons.attach_file_rounded, color: mine ? Colors.white : null),
          const SizedBox(width: 6),
          Flexible(child: Text(attachment?.fileName ?? '', style: TextStyle(color: mine ? Colors.white : null))),
        ]);
    }
  }

  Widget _image(BuildContext context) {
    final Widget picture = local != null
        ? Image.memory(local!.bytes, fit: BoxFit.cover)
        : Image.network(
            ChatService.instance.fileUrl(attachment!),
            fit: BoxFit.cover,
            loadingBuilder: (context, child, progress) =>
                progress == null ? child : const Center(child: CircularProgressIndicator(strokeWidth: 2)),
            errorBuilder: (context, error, stack) => const Center(child: Icon(Icons.broken_image_rounded, size: 36, color: Colors.white54)),
          );
    return GestureDetector(
      onTap: attachment == null ? null : () => _open(context, ChatImageViewer(url: ChatService.instance.fileUrl(attachment!))),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: Container(
          width: 230,
          height: 230,
          color: Colors.black12,
          child: Stack(fit: StackFit.expand, children: [picture, if (sending) _sendingOverlay()]),
        ),
      ),
    );
  }

  Widget _video(BuildContext context) => GestureDetector(
        onTap: attachment == null ? null : () => _open(context, ChatVideoScreen(url: ChatService.instance.fileUrl(attachment!))),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(12),
          child: Container(
            width: 230,
            height: 150,
            decoration: const BoxDecoration(
              gradient: LinearGradient(colors: [Color(0xFF1F2937), Color(0xFF111827)], begin: Alignment.topLeft, end: Alignment.bottomRight),
            ),
            child: Stack(fit: StackFit.expand, children: [
              Center(
                child: Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(color: Colors.white.withValues(alpha: .18), shape: BoxShape.circle),
                  child: const Icon(Icons.play_arrow_rounded, size: 40, color: Colors.white),
                ),
              ),
              PositionedDirectional(
                start: 10,
                bottom: 8,
                child: Row(children: [
                  const Icon(Icons.videocam_rounded, size: 16, color: Colors.white70),
                  const SizedBox(width: 4),
                  Text(_size(attachment?.sizeInBytes ?? local?.bytes.length ?? 0),
                      style: const TextStyle(color: Colors.white70, fontSize: 11.5)),
                ]),
              ),
              if (sending) _sendingOverlay(),
            ]),
          ),
        ),
      );

  /// A voice note on its way: its length and a spinner
  Widget _voiceSending(BuildContext context) {
    final color = mine ? Colors.white : AppTheme.blue;
    return SizedBox(
      width: 210,
      child: Row(children: [
        SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2, color: color)),
        const SizedBox(width: 10),
        Icon(Icons.mic_rounded, color: color, size: 20),
        const SizedBox(width: 6),
        Text(chatDuration(local?.duration ?? Duration.zero), style: TextStyle(color: color, fontWeight: FontWeight.w600)),
      ]),
    );
  }

  static Widget _sendingOverlay() => Container(
        color: Colors.black38,
        child: const Center(child: SizedBox(width: 30, height: 30, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))),
      );

  static String _size(int bytes) =>
      bytes >= 1024 * 1024 ? '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB' : '${(bytes / 1024).ceil()} KB';

  static void _open(BuildContext context, Widget screen) =>
      Navigator.of(context).push(MaterialPageRoute(fullscreenDialog: true, builder: (_) => screen));
}

/// A voice note: play / pause, progress you can drag, and the time
class ChatVoicePlayer extends StatefulWidget {
  const ChatVoicePlayer({super.key, required this.url, required this.mine});

  final String url;
  final bool mine;

  @override
  State<ChatVoicePlayer> createState() => _ChatVoicePlayerState();
}

class _ChatVoicePlayerState extends State<ChatVoicePlayer> {
  /// Created on the first play (a long chat does not open a player per note)
  AudioPlayer? _player;
  final List<StreamSubscription<Object?>> _subs = [];
  bool _loading = false;
  bool _playing = false;
  bool _failed = false;
  Duration _position = Duration.zero;
  Duration _length = Duration.zero;

  @override
  void dispose() {
    for (final s in _subs) {
      s.cancel();
    }
    _player?.dispose();
    super.dispose();
  }

  Future<void> _toggle() async {
    final existing = _player;
    if (existing != null) {
      if (_playing) {
        await existing.pause();
      } else {
        if (_length > Duration.zero && _position >= _length) await existing.seek(Duration.zero);
        unawaited(existing.play());
      }
      return;
    }

    final player = AudioPlayer();
    _player = player;
    setState(() {
      _loading = true;
      _failed = false;
    });
    _subs.add(player.positionStream.listen((p) {
      if (mounted) setState(() => _position = p);
    }));
    _subs.add(player.durationStream.listen((d) {
      if (mounted && d != null) setState(() => _length = d);
    }));
    _subs.add(player.playerStateStream.listen((s) {
      if (!mounted) return;
      final done = s.processingState == ProcessingState.completed;
      setState(() => _playing = s.playing && !done);
      if (done) {
        player.pause();
        player.seek(Duration.zero);
      }
    }));
    try {
      final length = await player.setUrl(widget.url);
      if (!mounted) return;
      setState(() {
        _loading = false;
        if (length != null) _length = length;
      });
      unawaited(player.play());
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loading = false;
        _failed = true;
      });
      _player = null;
      await player.dispose();
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = widget.mine ? Colors.white : AppTheme.blue;
    final muted = widget.mine ? Colors.white70 : context.palette.muted;
    final max = _length.inMilliseconds > 0 ? _length.inMilliseconds.toDouble() : 1.0;
    final value = _position.inMilliseconds.clamp(0, max.toInt()).toDouble();

    return SizedBox(
      width: 230,
      child: Row(
        children: [
          SizedBox(
            width: 40,
            height: 40,
            child: _loading
                ? Padding(padding: const EdgeInsets.all(9), child: CircularProgressIndicator(strokeWidth: 2, color: color))
                : IconButton(
                    padding: EdgeInsets.zero,
                    tooltip: _playing ? tr('إيقاف مؤقت', 'Pause') : tr('تشغيل', 'Play'),
                    onPressed: _toggle,
                    icon: Icon(_failed ? Icons.refresh_rounded : (_playing ? Icons.pause_rounded : Icons.play_arrow_rounded),
                        color: color, size: 30),
                  ),
          ),
          Expanded(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SliderTheme(
                  data: SliderTheme.of(context).copyWith(
                    trackHeight: 3,
                    thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 6),
                    overlayShape: const RoundSliderOverlayShape(overlayRadius: 12),
                    activeTrackColor: color,
                    inactiveTrackColor: color.withValues(alpha: .3),
                    thumbColor: color,
                  ),
                  child: SizedBox(
                    height: 24,
                    child: Slider(
                      value: value,
                      max: max,
                      onChanged: _player == null || _length == Duration.zero
                          ? null
                          : (v) => _player!.seek(Duration(milliseconds: v.toInt())),
                    ),
                  ),
                ),
                Padding(
                  padding: const EdgeInsetsDirectional.only(start: 12),
                  child: Text(
                    _failed
                        ? tr('تعذر التشغيل', 'Could not play')
                        : _player == null || (!_playing && _position == Duration.zero)
                            ? (_length > Duration.zero ? chatDuration(_length) : tr('رسالة صوتية', 'Voice message'))
                            : chatDuration(_position),
                    style: TextStyle(fontSize: 11, color: muted),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// A picture full screen: pinch to zoom
class ChatImageViewer extends StatelessWidget {
  const ChatImageViewer({super.key, required this.url});
  final String url;

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: Colors.black,
        appBar: AppBar(backgroundColor: Colors.black, foregroundColor: Colors.white, elevation: 0),
        body: InteractiveViewer(
          minScale: 1,
          maxScale: 5,
          child: Center(
            child: Image.network(
              url,
              fit: BoxFit.contain,
              loadingBuilder: (context, child, progress) =>
                  progress == null ? child : const Center(child: CircularProgressIndicator(color: Colors.white)),
              errorBuilder: (context, error, stack) => const Icon(Icons.broken_image_rounded, size: 60, color: Colors.white54),
            ),
          ),
        ),
      );
}

/// A video full screen: tap to play / pause, drag the bar to seek
class ChatVideoScreen extends StatefulWidget {
  const ChatVideoScreen({super.key, required this.url});
  final String url;

  @override
  State<ChatVideoScreen> createState() => _ChatVideoScreenState();
}

class _ChatVideoScreenState extends State<ChatVideoScreen> {
  late final VideoPlayerController _controller = VideoPlayerController.networkUrl(Uri.parse(widget.url));
  bool _failed = false;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_changed);
    _controller.initialize().then((_) {
      if (mounted) {
        setState(() {});
        _controller.play();
      }
    }).catchError((Object _) {
      if (mounted) setState(() => _failed = true);
    });
  }

  void _changed() {
    if (mounted) setState(() {});
  }

  @override
  void dispose() {
    _controller.removeListener(_changed);
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final value = _controller.value;
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(backgroundColor: Colors.black, foregroundColor: Colors.white, elevation: 0),
      body: _failed
          ? Center(
              child: Text(tr('تعذر تشغيل الفيديو.', 'Could not play the video.'), style: const TextStyle(color: Colors.white70)),
            )
          : !value.isInitialized
              ? const Center(child: CircularProgressIndicator(color: Colors.white))
              : Column(
                  children: [
                    Expanded(
                      child: GestureDetector(
                        onTap: () => value.isPlaying ? _controller.pause() : _controller.play(),
                        child: Center(
                          child: AspectRatio(
                            aspectRatio: value.aspectRatio == 0 ? 16 / 9 : value.aspectRatio,
                            child: Stack(alignment: Alignment.center, children: [
                              VideoPlayer(_controller),
                              if (!value.isPlaying) const Icon(Icons.play_circle_fill_rounded, size: 72, color: Colors.white70),
                            ]),
                          ),
                        ),
                      ),
                    ),
                    SafeArea(
                      top: false,
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                        child: Row(children: [
                          Text(chatDuration(value.position), style: const TextStyle(color: Colors.white70, fontSize: 12)),
                          const SizedBox(width: 10),
                          Expanded(
                            child: VideoProgressIndicator(_controller,
                                allowScrubbing: true,
                                colors: const VideoProgressColors(playedColor: Colors.white, bufferedColor: Colors.white38)),
                          ),
                          const SizedBox(width: 10),
                          Text(chatDuration(value.duration), style: const TextStyle(color: Colors.white70, fontSize: 12)),
                        ]),
                      ),
                    ),
                  ],
                ),
    );
  }
}
