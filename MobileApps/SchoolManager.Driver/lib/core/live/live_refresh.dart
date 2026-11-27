import 'dart:async';

import 'package:flutter/widgets.dart';

/// "The data may have changed": screens using [LiveRefreshMixin] reload quietly, so a record saved elsewhere
/// (by the school, a teacher, a driver…) shows without the user refreshing. It fires when:
/// - a push notification arrives while the app is open ([ping], called by the push service),
/// - the app comes back to the foreground,
/// - every [interval] while the app is open.
/// Not started under widget tests (no periodic timer there).
class LiveRefresh with WidgetsBindingObserver {
  LiveRefresh._();
  static final instance = LiveRefresh._();

  static const interval = Duration(seconds: 30);

  final _ticks = StreamController<void>.broadcast();
  Timer? _timer;
  bool _started = false;
  bool _foreground = true;

  Stream<void> get ticks => _ticks.stream;

  /// Widget tests use a test binding: no timer, no lifecycle hook
  static bool get _isTest => WidgetsBinding.instance is! WidgetsFlutterBinding;

  void _ensureStarted() {
    if (_started || _isTest) return;
    _started = true;
    WidgetsBinding.instance.addObserver(this);
    _timer = Timer.periodic(interval, (_) {
      if (_foreground) ping();
    });
  }

  /// Ask the open screen to reload now
  void ping() {
    if (!_ticks.isClosed) _ticks.add(null);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    final wasForeground = _foreground;
    _foreground = state == AppLifecycleState.resumed;
    if (_foreground && !wasForeground) ping();
  }

  /// For completeness (the app lives as long as the process)
  void stop() {
    _timer?.cancel();
    if (_started) WidgetsBinding.instance.removeObserver(this);
    _started = false;
  }
}

/// A screen that reloads its data quietly on [LiveRefresh] — only while it is the visible page, and never twice at
/// once. Screens where the user is entering data do not use it (a reload would drop what they typed).
mixin LiveRefreshMixin<T extends StatefulWidget> on State<T> {
  /// Load the page's data again without clearing what is shown
  Future<void> onLiveRefresh();

  StreamSubscription<void>? _liveSubscription;
  bool _liveBusy = false;

  @override
  void initState() {
    super.initState();
    LiveRefresh.instance._ensureStarted();
    _liveSubscription = LiveRefresh.instance.ticks.listen((_) => _onLiveTick());
  }

  Future<void> _onLiveTick() async {
    if (!mounted || _liveBusy) return;
    // Only the page on top (not one under another page or a sheet)
    final route = ModalRoute.of(context);
    if (route != null && !route.isCurrent) return;
    _liveBusy = true;
    try {
      await onLiveRefresh();
    } catch (_) {
      // A failed quiet reload keeps what is shown; the next one tries again
    } finally {
      _liveBusy = false;
    }
  }

  @override
  void dispose() {
    _liveSubscription?.cancel();
    super.dispose();
  }
}
