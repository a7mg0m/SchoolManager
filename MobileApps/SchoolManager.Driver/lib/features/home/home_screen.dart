import 'package:flutter/material.dart';

import '../../core/auth/auth_service.dart';
import '../../core/i18n/app_language.dart';
import '../../core/i18n/language_switch.dart';
import '../../core/push/push_service.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/theme_controller.dart';
import '../notifications/notifications_screen.dart';
import '../notifications/notifications_service.dart';
import '../profile/edit_profile_screen.dart';
import '../profile/profile_service.dart';
import '../trips/location_tracker.dart';
import '../trips/trip_screens.dart';
import '../trips/trip_service.dart';
import '../trips/trip_widgets.dart';

/// Driver home: today's trips of the bus (start / finish), all trips, and the account (profile, language, theme)
class HomeScreen extends StatefulWidget {
  const HomeScreen({
    super.key,
    this.loadBus,
    this.loadTrips,
    this.loadProfile,
    this.createTrip,
    this.startTrip,
    this.completeTrip,
    this.cancelTrip,
    this.loadTrip,
    this.loadStudents,
    this.markStudent,
    this.tracker,
    this.loadNotifications,
  });

  /// Default to the API (tests pass their own)
  final Future<DriverBus> Function()? loadBus;
  final Future<List<Trip>> Function()? loadTrips;
  final Future<MyProfile?> Function()? loadProfile;
  final Future<void> Function(DateTime date, TripType type)? createTrip;
  final Future<void> Function(String id)? startTrip;
  final Future<void> Function(String id)? completeTrip;
  final Future<void> Function(String id)? cancelTrip;
  final Future<Trip> Function(String id)? loadTrip;
  final Future<List<TripRider>> Function(String tripId)? loadStudents;
  final Future<void> Function(String tripId, String studentId, TripStudentStatus status)? markStudent;

  /// Shares the bus's position while a trip runs (tests pass their own)
  final LocationTracker? tracker;

  /// School notifications for everyone / for my bus (tests pass their own)
  final Future<List<AppNotice>> Function()? loadNotifications;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _tab = 0;
  DriverBus? _bus;
  List<Trip> _trips = const [];
  MyProfile? _profile;
  bool _loading = true;
  String? _error;

  /// Trip whose start / finish is being sent
  String? _busyTripId;

  /// Tabs: today, trips, notifications, more
  static const _notificationsTab = 2;

  /// The notifications tab is built the first time it is opened (then kept); a new key reloads it
  bool _notificationsOpened = false;
  int _notificationsVersion = 0;

  LocationTracker get _tracker => widget.tracker ?? LocationTracker.instance;

  @override
  void initState() {
    super.initState();
    _tracker.addListener(_onTracker);
    // A tapped push notification (now, or the one that opened the app)
    PushService.instance.openRoute.addListener(_onPushRoute);
    WidgetsBinding.instance.addPostFrameCallback((_) => _onPushRoute());
    _refresh();
  }

  @override
  void dispose() {
    _tracker.removeListener(_onTracker);
    PushService.instance.openRoute.removeListener(_onPushRoute);
    super.dispose();
  }

  /// A school notification push opens the notifications tab (fresh); other pushes just open the app
  void _onPushRoute() {
    final route = PushService.instance.openRoute.value;
    if (route == null || !mounted) return;
    PushService.instance.openRoute.value = null;
    if (route.startsWith('/notifications')) {
      _navKey.currentState?.popUntil((r) => r.isFirst);
      _selectTab(_notificationsTab, reload: true);
    }
  }

  void _selectTab(int index, {bool reload = false}) {
    setState(() {
      if (index == _notificationsTab) {
        if (_notificationsOpened && reload) _notificationsVersion++;
        _notificationsOpened = true;
      }
      _tab = index;
    });
  }

  void _onTracker() {
    if (mounted) setState(() {});
  }

  Future<void> _refresh() async {
    setState(() => _error = null);
    try {
      final results = await Future.wait<Object?>([
        (widget.loadBus ?? TripService.instance.getMyBus)(),
        (widget.loadTrips ?? () => TripService.instance.getTrips())(),
        (widget.loadProfile ?? ProfileService.instance.getMyProfile)().catchError((_) => null),
      ]);
      if (!mounted) return;
      setState(() {
        _bus = results[0] as DriverBus;
        _trips = results[1] as List<Trip>;
        _profile = results[2] as MyProfile? ?? _profile;
        _loading = false;
      });
      // A trip on the road: its position is shared automatically
      _tracker.follow(_trips);
    } on SessionExpiredException {
      await _logout();
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = '$e';
        _loading = false;
      });
    }
  }

  Future<void> _logout() async {
    _tracker.stop();
    await AuthService.instance.logout();
    if (mounted) Navigator.of(context).pushNamedAndRemoveUntil(AppRoutes.login, (_) => false);
  }

  void _snack(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  // ---------------------------------------------------------------- trips

  List<Trip> get _todayTrips {
    final today = DateTime.now();
    return _trips.where((t) => t.isOn(today)).toList()..sort((a, b) => a.type.index.compareTo(b.type.index));
  }

  Future<void> _newTrip() async {
    final created = await showNewTripSheet(context, create: widget.createTrip ?? (d, t) => TripService.instance.createTrip(d, t));
    if (created == null) return;
    _snack(tr('تم إنشاء الرحلة.', 'The trip was created.'));
    await _refresh();
  }

  Future<void> _action(Trip trip, Future<void> Function(String id) action, String done, {String? confirm}) async {
    if (confirm != null) {
      final ok = await showDialog<bool>(
        context: context,
        builder: (dialog) => AlertDialog(
          content: Text(confirm),
          actions: [
            TextButton(onPressed: () => Navigator.pop(dialog, false), child: Text(tr('تراجع', 'Back'))),
            FilledButton(onPressed: () => Navigator.pop(dialog, true), child: Text(tr('تأكيد', 'Confirm'))),
          ],
        ),
      );
      if (ok != true) return;
    }
    setState(() => _busyTripId = trip.id);
    try {
      await action(trip.id);
      _snack(done);
      await _refresh();
    } on SessionExpiredException {
      await _logout();
    } catch (e) {
      _snack('$e');
    } finally {
      if (mounted) setState(() => _busyTripId = null);
    }
  }

  /// Starts the trip, then opens it so the driver can mark the students
  Future<void> _start(Trip trip) async {
    await _action(trip, widget.startTrip ?? TripService.instance.start, tr('بدأت الرحلة، رحلة موفقة!', 'The trip has started. Drive safely!'));
    final started = _trips.where((t) => t.id == trip.id && t.status == TripStatus.inProgress);
    if (mounted && started.isNotEmpty) await _openTrip(started.first);
  }

  void _complete(Trip trip) => _action(trip, widget.completeTrip ?? TripService.instance.complete,
      tr('انتهت الرحلة.', 'The trip is finished.'),
      confirm: tr('هل وصلت؟ سيتم إنهاء الرحلة.', 'Have you arrived? The trip will be finished.'));

  Future<void> _openTrip(Trip trip) async {
    final changed = await _nav.push<bool>(MaterialPageRoute(
      builder: (_) => TripDetailsScreen(
        tripId: trip.id,
        loadTrip: widget.loadTrip,
        startTrip: widget.startTrip,
        completeTrip: widget.completeTrip,
        cancelTrip: widget.cancelTrip,
        loadStudents: widget.loadStudents,
        markStudent: widget.markStudent,
        tracker: widget.tracker,
      ),
    ));
    if (changed == true) await _refresh();
  }

  Widget _tripCard(Trip trip, {bool showDate = false}) => TripCard(
        trip: trip,
        showDate: showDate,
        busy: _busyTripId == trip.id,
        onTap: () => _openTrip(trip),
        onStart: trip.canStart && trip.isOn(DateTime.now()) ? () => _start(trip) : null,
        onComplete: trip.canComplete ? () => _complete(trip) : null,
      );

  // ---------------------------------------------------------------- profile

  Future<void> _editProfile() async {
    var current = _profile;
    current ??= await ProfileService.instance.getMyProfile().catchError((_) => null);
    if (!mounted) return;
    if (current == null) {
      _snack(tr('تعذر تحميل الملف الشخصي، حاول مرة أخرى.', 'Could not load your profile. Please try again.'));
      return;
    }
    final saved = await _nav.push<bool>(MaterialPageRoute(builder: (_) => EditProfileScreen(profile: current!)));
    if (!mounted) return;
    if (AuthService.instance.session?.isExpired ?? true) {
      await _logout();
      return;
    }
    if (saved == true) await _refresh();
  }

  String get _name => _profile?.displayName ?? AuthService.instance.session?.displayName ?? tr('السائق', 'Driver');

  // ---------------------------------------------------------------- UI

  /// Pages opened from the tabs (trip details, map, profile, …) open in this navigator,
  /// so the bottom bar (Today / Trips / Notifications / More) stays under every page
  final _navKey = GlobalKey<NavigatorState>();

  NavigatorState get _nav => _navKey.currentState!;

  /// A tab: close the pages opened above the tabs, then show it
  void _onTab(int index) {
    _navKey.currentState?.popUntil((r) => r.isFirst);
    _selectTab(index);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: NavigatorPopHandler<Object?>(
        // Phone back button: close the open page first
        onPopWithResult: (_) => _nav.maybePop(),
        child: Navigator(
          key: _navKey,
          onGenerateRoute: (_) => MaterialPageRoute(builder: (_) => _tabsPage()),
        ),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: _onTab,
        destinations: [
          NavigationDestination(icon: const Icon(Icons.today_outlined), selectedIcon: const Icon(Icons.today_rounded), label: tr('اليوم', 'Today')),
          NavigationDestination(icon: const Icon(Icons.route_outlined), selectedIcon: const Icon(Icons.route_rounded), label: tr('الرحلات', 'Trips')),
          NavigationDestination(
            icon: const Icon(Icons.notifications_none_rounded),
            selectedIcon: const Icon(Icons.notifications_rounded),
            label: tr('الإشعارات', 'Notifications'),
          ),
          NavigationDestination(icon: const Icon(Icons.menu_rounded), label: tr('المزيد', 'More')),
        ],
      ),
    );
  }

  /// The tabs themselves (first page of the inner navigator; rebuilt with this state)
  Widget _tabsPage() {
    final pages = [_todayTab(), _tripsTab(), _notificationsPage(), _moreTab()];
    return Scaffold(
      body: SafeArea(
        top: _tab != 0,
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null && _bus == null
                ? _errorView()
                : IndexedStack(index: _tab, children: pages),
      ),
      floatingActionButton: !_loading && _bus != null && _tab < 2
          ? FloatingActionButton.extended(
              key: const ValueKey('new-trip'),
              onPressed: _newTrip,
              backgroundColor: AppTheme.blue,
              foregroundColor: Colors.white,
              icon: const Icon(Icons.add_road_rounded),
              label: Text(tr('رحلة جديدة', 'New trip')),
            )
          : null,
    );
  }

  Widget _errorView() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.cloud_off_rounded, size: 56, color: context.palette.muted),
              const SizedBox(height: 12),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              FilledButton(onPressed: _refresh, child: Text(tr('إعادة المحاولة', 'Retry'))),
              TextButton(onPressed: _logout, child: Text(tr('تسجيل الخروج', 'Sign out'))),
            ],
          ),
        ),
      );

  // ---- Today

  Widget _todayTab() {
    final palette = context.palette;
    final today = _todayTrips;
    final running = _trips.where((t) => t.status == TripStatus.inProgress).toList();
    return RefreshIndicator(
      onRefresh: _refresh,
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
          _header(),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _trackingBanner(),
                // A trip still running from another day comes first
                for (final t in running.where((t) => !t.isOn(DateTime.now()))) _tripCard(t, showDate: true),
                Text(tr('رحلات اليوم', "Today's trips"), style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: palette.ink)),
                const SizedBox(height: 10),
                if (today.isEmpty)
                  _empty(
                    Icons.directions_bus_outlined,
                    tr('لا توجد رحلات اليوم', 'No trips today'),
                    tr('أنشئ رحلة الذهاب أو العودة لتبدأ.', 'Create the trip to school or home to get started.'),
                  )
                else
                  for (final t in today) _tripCard(t),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _header() {
    final bus = _bus;
    final image = _profile?.image;
    return Container(
      padding: EdgeInsets.fromLTRB(20, MediaQuery.of(context).padding.top + 18, 20, 22),
      decoration: const BoxDecoration(
        gradient: LinearGradient(colors: [AppTheme.navy, AppTheme.blue], begin: Alignment.topLeft, end: Alignment.bottomRight),
        borderRadius: BorderRadius.vertical(bottom: Radius.circular(28)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(tr('مرحباً،', 'Hello,'), style: const TextStyle(color: Colors.white70)),
                    Text(_name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(color: Colors.white, fontSize: 21, fontWeight: FontWeight.bold)),
                  ],
                ),
              ),
              GestureDetector(
                onTap: _editProfile,
                child: CircleAvatar(
                  radius: 24,
                  backgroundColor: Colors.white24,
                  backgroundImage: image != null ? MemoryImage(image) : null,
                  child: image == null ? const Icon(Icons.person_rounded, color: Colors.white) : null,
                ),
              ),
            ],
          ),
          if (bus != null) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(color: Colors.white.withValues(alpha: .14), borderRadius: BorderRadius.circular(18)),
              child: Row(
                children: [
                  Container(
                    width: 46,
                    height: 46,
                    decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14)),
                    child: const Icon(Icons.directions_bus_rounded, color: AppTheme.blue),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(tr('حافلة رقم ${bus.number}', 'Bus ${bus.number}'),
                            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                        Text(
                          [bus.plateNumber, if (bus.routeName.isNotEmpty) bus.routeName].join(' · '),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(color: Colors.white70, fontSize: 12.5),
                        ),
                      ],
                    ),
                  ),
                  Column(
                    children: [
                      Text('${bus.capacity}', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 18)),
                      Text(tr('مقعد', 'seats'), style: const TextStyle(color: Colors.white70, fontSize: 11)),
                    ],
                  ),
                ],
              ),
            ),
            if (!bus.isActive) ...[
              const SizedBox(height: 10),
              Text(tr('الحافلة غير مفعّلة حالياً.', 'The bus is not active at the moment.'),
                  style: const TextStyle(color: Color(0xFFFFD27A), fontWeight: FontWeight.w600)),
            ],
          ],
        ],
      ),
    );
  }

  /// Sharing the bus's position: when it was last sent, or why it does not work (with a retry)
  Widget _trackingBanner() {
    final t = _tracker;
    if (t.tripId == null) return const SizedBox.shrink();
    final ok = t.error == null;
    final color = ok ? const Color(0xFF1F9D57) : const Color(0xFFC0392B);
    final sent = t.lastSentAt;
    return Container(
      key: const ValueKey('tracking-banner'),
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(color: color.withValues(alpha: .1), borderRadius: BorderRadius.circular(14)),
      child: Row(
        children: [
          Icon(ok ? Icons.my_location_rounded : Icons.location_disabled_rounded, color: color, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              !ok
                  ? t.error!
                  : sent == null
                      ? tr('جارٍ تحديد موقع الحافلة…', "Getting the bus's position…")
                      : tr('يتم مشاركة موقع الحافلة تلقائياً · آخر تحديث ${formatTime(sent)}',
                          "The bus's position is shared automatically · last sent ${formatTime(sent)}"),
              style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 12.5),
            ),
          ),
          if (!ok)
            TextButton(onPressed: t.retry, child: Text(tr('إعادة', 'Retry'))),
        ],
      ),
    );
  }

  Widget _empty(IconData icon, String title, String text) {
    final palette = context.palette;
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 30, horizontal: 20),
      decoration: BoxDecoration(color: palette.card, borderRadius: BorderRadius.circular(18), border: Border.all(color: palette.line)),
      child: Column(
        children: [
          Icon(icon, size: 48, color: palette.muted),
          const SizedBox(height: 10),
          Text(title, style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
          const SizedBox(height: 4),
          Text(text, textAlign: TextAlign.center, style: TextStyle(color: palette.muted)),
        ],
      ),
    );
  }

  // ---- Trips

  Widget _tripsTab() {
    final palette = context.palette;
    final today = DateTime(DateTime.now().year, DateTime.now().month, DateTime.now().day);
    final upcoming = _trips.where((t) => !t.date.isBefore(today)).toList()..sort((a, b) => a.date.compareTo(b.date));
    final past = _trips.where((t) => t.date.isBefore(today)).toList();
    return RefreshIndicator(
      onRefresh: _refresh,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
        children: [
          Text(tr('الرحلات', 'Trips'), style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: palette.ink)),
          Text(tr('آخر 14 يوماً والأسبوع القادم', 'The last 14 days and the coming week'), style: TextStyle(color: palette.muted)),
          const SizedBox(height: 14),
          if (_trips.isEmpty)
            _empty(Icons.route_outlined, tr('لا توجد رحلات', 'No trips'),
                tr('الرحلات التي تنشئها تظهر هنا.', 'The trips you create show up here.')),
          if (upcoming.isNotEmpty) ...[
            _section(tr('اليوم والقادمة', 'Today and upcoming')),
            for (final t in upcoming) _tripCard(t, showDate: true),
          ],
          if (past.isNotEmpty) ...[
            _section(tr('السابقة', 'Past')),
            for (final t in past) _tripCard(t, showDate: true),
          ],
        ],
      ),
    );
  }

  Widget _section(String title) => Padding(
        padding: const EdgeInsets.only(top: 6, bottom: 8),
        child: Text(title, style: TextStyle(fontWeight: FontWeight.w700, color: context.palette.muted)),
      );

  // ---- More

  // ---- Notifications

  Widget _notificationsPage() {
    if (!_notificationsOpened) return const SizedBox.shrink();
    return NotificationsScreen(
      key: ValueKey('notifications-$_notificationsVersion'),
      loadNotices: widget.loadNotifications ?? NotificationsService.instance.getMine,
      targetedLabel: tr('لحافلتك', 'For your bus'),
      onSessionExpired: _logout,
    );
  }

  Widget _moreTab() {
    final palette = context.palette;
    final bus = _bus;
    final image = _profile?.image;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          elevation: 0,
          color: palette.card,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18), side: BorderSide(color: palette.line)),
          child: ListTile(
            key: const ValueKey('edit-profile'),
            contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
            leading: CircleAvatar(
              radius: 24,
              backgroundColor: AppTheme.blue.withValues(alpha: .12),
              backgroundImage: image != null ? MemoryImage(image) : null,
              child: image == null ? const Icon(Icons.person_rounded, color: AppTheme.blue) : null,
            ),
            title: Text(_name, style: const TextStyle(fontWeight: FontWeight.bold)),
            subtitle: Text(tr('تعديل الملف الشخصي وكلمة المرور', 'Edit profile and password')),
            trailing: const Icon(Icons.chevron_right_rounded),
            onTap: _editProfile,
          ),
        ),
        if (bus != null && bus.stops.isNotEmpty) ...[
          const SizedBox(height: 12),
          Card(
            elevation: 0,
            color: palette.card,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18), side: BorderSide(color: palette.line)),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(tr('خط السير', 'Route'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                  if (bus.routeName.isNotEmpty) Text(bus.routeName, style: TextStyle(color: palette.muted)),
                  const SizedBox(height: 10),
                  for (var i = 0; i < bus.stops.length; i++)
                    Row(
                      children: [
                        Column(
                          children: [
                            Container(
                              width: 24,
                              height: 24,
                              alignment: Alignment.center,
                              decoration: const BoxDecoration(color: AppTheme.blue, shape: BoxShape.circle),
                              child: Text('${i + 1}', style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
                            ),
                            if (i < bus.stops.length - 1) Container(width: 2, height: 18, color: AppTheme.blue.withValues(alpha: .3)),
                          ],
                        ),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Padding(
                            padding: EdgeInsets.only(bottom: i < bus.stops.length - 1 ? 18 : 0),
                            child: Text(bus.stops[i].name, style: TextStyle(color: palette.ink)),
                          ),
                        ),
                      ],
                    ),
                ],
              ),
            ),
          ),
        ],
        const SizedBox(height: 12),
        Card(
          elevation: 0,
          color: palette.card,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18), side: BorderSide(color: palette.line)),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('اللغة', 'Language'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                const SizedBox(height: 8),
                const LanguageSwitch(),
                const SizedBox(height: 16),
                Text(tr('المظهر', 'Appearance'), style: TextStyle(fontWeight: FontWeight.bold, color: palette.ink)),
                const SizedBox(height: 8),
                ValueListenableBuilder<ThemeMode>(
                  valueListenable: ThemeController.instance,
                  builder: (context, mode, _) => SegmentedButton<ThemeMode>(
                    segments: [
                      ButtonSegment(value: ThemeMode.light, icon: const Icon(Icons.light_mode_outlined), label: Text(tr('فاتح', 'Light'))),
                      ButtonSegment(value: ThemeMode.dark, icon: const Icon(Icons.dark_mode_outlined), label: Text(tr('داكن', 'Dark'))),
                      ButtonSegment(value: ThemeMode.system, icon: const Icon(Icons.brightness_auto_outlined), label: Text(tr('تلقائي', 'Auto'))),
                    ],
                    selected: {mode},
                    onSelectionChanged: (s) => ThemeController.instance.setMode(s.first),
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        OutlinedButton.icon(
          key: const ValueKey('logout'),
          onPressed: _logout,
          style: OutlinedButton.styleFrom(
            foregroundColor: const Color(0xFFC0392B),
            minimumSize: const Size.fromHeight(48),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          ),
          icon: const Icon(Icons.logout_rounded),
          label: Text(tr('تسجيل الخروج', 'Sign out')),
        ),
      ],
    );
  }
}
