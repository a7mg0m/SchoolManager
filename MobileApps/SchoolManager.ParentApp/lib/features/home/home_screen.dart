import 'package:flutter/material.dart';
import '../../core/auth/auth_service.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/theme_controller.dart';
import '../../core/i18n/app_language.dart';
import '../../core/i18n/language_switch.dart';
import '../../data/demo_data.dart';
import '../children/children_service.dart';
import '../profile/edit_profile_screen.dart';
import '../profile/profile_service.dart';
import '../schedule/schedule_service.dart';
import '../schedule/schedule_format.dart';
import '../schedule/timetable_screen.dart';
import '../fees/fees_screen.dart';
import '../fees/fees_service.dart';
import '../exams/exams_screen.dart';
import '../exams/exams_service.dart';
import '../events/events_screen.dart';
import '../events/events_service.dart';
import '../school/school_info_screen.dart';
import '../school/school_info_service.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen(
      {super.key,
      this.loadChildren,
      this.loadProfile,
      this.loadTimetable,
      this.loadFees,
      this.loadExams,
      this.loadEvents,
      this.loadSchoolInfo});

  /// Loads the parent's children; defaults to the API (tests pass their own)
  final Future<List<Child>> Function()? loadChildren;

  /// Loads the signed-in user's profile (name + picture); defaults to the API
  final Future<MyProfile?> Function()? loadProfile;

  /// Loads a child's lessons for today / the next school day; defaults to the API
  final Future<ChildTimetable> Function(String studentId)? loadTimetable;

  /// Loads a child's fees; defaults to the API
  final Future<List<StudentFeeItem>> Function(String studentId)? loadFees;

  /// Loads a child's exams with its results; defaults to the API
  final Future<List<ChildExam>> Function(String studentId)? loadExams;

  /// Loads the school events of a child; defaults to the API
  final Future<List<ChildEvent>> Function(String studentId)? loadEvents;

  /// Loads the school information (About the school page); defaults to the API
  final Future<SchoolInfo?> Function()? loadSchoolInfo;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<Child> children = const [];
  Child? selectedChild;
  bool isLoadingChildren = true;
  String? childrenError;
  int selectedTab = 0;
  MyProfile? profile;

  /// For the logo in the header
  SchoolInfo? schoolInfo;

  @override
  void initState() {
    super.initState();
    _loadChildren();
    _loadProfile();
    _loadSchoolInfo();
    AppLanguage.instance.addListener(_onLanguageChanged);
  }

  @override
  void dispose() {
    AppLanguage.instance.removeListener(_onLanguageChanged);
    super.dispose();
  }

  /// Grade / class names come from the API in the chosen language: load them again
  void _onLanguageChanged() {
    if (mounted) _loadChildren();
  }

  /// School logo for the header; on failure (or no logo) the header keeps the school icon
  Future<void> _loadSchoolInfo() async {
    try {
      final loader =
          widget.loadSchoolInfo ?? SchoolInfoService.instance.getSchoolInfo;
      final result = await loader();
      if (mounted) setState(() => schoolInfo = result);
    } on SchoolInfoException {
      // Keep the icon
    }
  }

  void _openSchoolInfo() {
    _nav.push(MaterialPageRoute(
      builder: (_) => SchoolInfoScreen(loadSchoolInfo: widget.loadSchoolInfo),
    ));
  }

  /// Logo from the school info (white circle), else the school icon; tap: About the school
  Widget _schoolLogo() {
    final logo = schoolInfo?.logo;
    final icon = const CircleAvatar(
      radius: 25,
      backgroundColor: Colors.white24,
      child: Icon(Icons.school_rounded, color: Colors.white, size: 28),
    );
    return Tooltip(
      message: schoolInfo?.name ?? tr('عن المدرسة', 'About the school'),
      child: GestureDetector(
        key: const ValueKey('school-logo'),
        onTap: _openSchoolInfo,
        child: logo == null
            ? icon
            : Container(
                width: 50,
                height: 50,
                padding: const EdgeInsets.all(4),
                decoration: const BoxDecoration(
                  color: Colors.white,
                  shape: BoxShape.circle,
                  boxShadow: [BoxShadow(color: Color(0x33000000), blurRadius: 8, offset: Offset(0, 3))],
                ),
                child: ClipOval(
                  child: Image.memory(
                    logo,
                    fit: BoxFit.contain,
                    gaplessPlayback: true,
                    errorBuilder: (_, __, ___) => const Icon(Icons.school_rounded, color: AppTheme.blue, size: 26),
                  ),
                ),
              ),
      ),
    );
  }

  /// Name and picture from My Profile; on failure the header keeps the login name and the person icon
  Future<void> _loadProfile() async {
    try {
      final loader = widget.loadProfile ?? ProfileService.instance.getMyProfile;
      final result = await loader();
      if (!mounted || result == null) return;
      setState(() => profile = result);
      await AuthService.instance.updateProfileDetails(
        firstName: result.firstName,
        lastName: result.lastName,
        email: result.email,
      );
    } on SessionExpiredException {
      if (mounted) await _logout();
    }
  }

  Future<void> _refresh() => Future.wait([_loadChildren(), _loadProfile()]);

  /// Edit profile screen; back on the home tab with all data reloaded when something was saved
  Future<void> _openEditProfile() async {
    var current = profile;
    if (current == null) {
      // Not loaded yet (or it failed): try once more before opening
      await _loadProfile();
      current = profile;
    }
    if (!mounted) return;
    if (current == null) {
      _showMessage(tr('تعذر تحميل الملف الشخصي، حاول مرة أخرى.', 'Could not load your profile. Please try again.'));
      return;
    }

    final saved = await _nav.push<bool>(
      MaterialPageRoute(builder: (_) => EditProfileScreen(profile: current!)),
    );
    if (!mounted) return;
    // The token may have expired while editing
    if (AuthService.instance.session?.isExpired ?? true) {
      await _logout();
      return;
    }
    if (saved == true) {
      // Show the home tab with fresh data (profile header and children)
      setState(() => selectedTab = 0);
      await _refresh();
    }
  }

  /// Children of the parent in the user profile that signed in (parent id from the token)
  Future<void> _loadChildren() async {
    setState(() {
      isLoadingChildren = true;
      childrenError = null;
    });
    try {
      final loader =
          widget.loadChildren ?? ChildrenService.instance.getMyChildren;
      final result = await loader();
      if (!mounted) return;
      setState(() {
        children = result;
        // Keep the selected child after a refresh when it is still there
        selectedChild =
            result.where((c) => c.id == selectedChild?.id).firstOrNull ??
                result.firstOrNull;
        isLoadingChildren = false;
      });
      // Today card and live tiles of the selected child
      if (selectedChild != null) _loadLive(selectedChild!);
    } on ChildrenException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired) {
        await _logout();
        return;
      }
      setState(() {
        isLoadingChildren = false;
        childrenError = e.message;
      });
    }
  }

  // ----- Live data of the selected child (today card and bento tiles) -----

  ChildTimetable? _timetable;
  List<ChildExam>? _exams;
  List<StudentFeeItem>? _fees;
  List<ChildEvent>? _events;

  /// Child the live data belongs to (a quick switch ignores late answers)
  String? _liveFor;

  /// Timetable, exams, fees and events of [child], each on its own: a failure only leaves that tile plain
  Future<void> _loadLive(Child child) async {
    _liveFor = child.id;
    setState(() {
      _timetable = null;
      _exams = null;
      _fees = null;
      _events = null;
    });

    Future<void> load<T>(Future<T> Function(String studentId) loader, void Function(T value) apply) async {
      try {
        final result = await loader(child.id);
        if (mounted && _liveFor == child.id) setState(() => apply(result));
      } catch (_) {
        // The tile shows its plain text; the page itself reports the error
      }
    }

    await Future.wait([
      load<ChildTimetable>(widget.loadTimetable ?? ScheduleService.instance.getChildTimetable, (r) => _timetable = r),
      load<List<ChildExam>>(widget.loadExams ?? ExamsService.instance.getChildExams, (r) => _exams = r),
      load<List<StudentFeeItem>>(widget.loadFees ?? FeesService.instance.getChildFees, (r) => _fees = r),
      load<List<ChildEvent>>(widget.loadEvents ?? EventsService.instance.getChildEvents, (r) => _events = r),
    ]);
  }

  void _selectChild(Child child) {
    if (child.id == selectedChild?.id) return;
    setState(() => selectedChild = child);
    _loadLive(child);
  }

  /// Pages opened from Home (timetable, exams, profile, …) open in this navigator,
  /// so the bottom bar stays pinned under every page
  final _navKey = GlobalKey<NavigatorState>();

  NavigatorState get _nav => _navKey.currentState!;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: NavigatorPopHandler<Object?>(
        // Phone back button: close the open page first
        onPopWithResult: (_) => _nav.maybePop(),
        child: Navigator(
          key: _navKey,
          onGenerateRoute: (_) => MaterialPageRoute(builder: (_) => _homePage()),
        ),
      ),
      bottomNavigationBar: _bottomNavigation(),
    );
  }

  /// The home page itself (first page of the inner navigator; rebuilt with this state)
  Widget _homePage() {
    return Scaffold(
      body: RefreshIndicator(
        onRefresh: _refresh,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: EdgeInsets.zero,
          children: [
            _buildHeader(),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 18, 16, 24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // The important announcement first, then today, then the services
                  _Appear(order: 0, child: _buildAnnouncement()),
                  if (_timetable != null) ...[
                    const SizedBox(height: 14),
                    _Appear(order: 1, child: _todayCard(_timetable!)),
                  ],
                  const SizedBox(height: 22),
                  _sectionTitle(tr('الخدمات', 'Services')),
                  _buildServices(),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ----- Header: school logo, greeting, profile picture and the children as stories -----

  Widget _buildHeader() {
    final hour = DateTime.now().hour;
    final greeting = hour < 12 ? tr('صباح الخير ☀️', 'Good morning ☀️') : tr('مساء الخير 🌙', 'Good evening 🌙');
    return ClipRRect(
      borderRadius: const BorderRadius.vertical(bottom: Radius.circular(36)),
      child: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            colors: [Color(0xFF2F80ED), Color(0xFF1046A8), Color(0xFF3B1F94)],
            begin: AlignmentDirectional.topStart,
            end: AlignmentDirectional.bottomEnd,
          ),
        ),
        child: Stack(
          children: [
            // Soft light circles
            PositionedDirectional(top: -60, end: -40, child: _glow(190, .10)),
            PositionedDirectional(top: 70, start: -70, child: _glow(160, .07)),
            PositionedDirectional(bottom: -50, end: 60, child: _glow(120, .08)),
            SafeArea(
              bottom: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(18, 14, 18, 22),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        _schoolLogo(),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(greeting,
                                  style: TextStyle(color: Colors.white.withValues(alpha: .8), fontSize: 13)),
                              const SizedBox(height: 2),
                              Text(
                                // Profile name, else the name saved at login (demo name only without a session, e.g. tests)
                                profile?.displayName ?? AuthService.instance.session?.displayName ?? parentName,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: Colors.white, fontSize: 21, fontWeight: FontWeight.bold),
                              ),
                            ],
                          ),
                        ),
                        Stack(
                          clipBehavior: Clip.none,
                          children: [
                            _profileAvatar(),
                            PositionedDirectional(
                              top: -3,
                              end: -3,
                              child: Container(
                                padding: const EdgeInsets.all(5),
                                decoration: BoxDecoration(
                                  color: const Color(0xFFFF4D6D),
                                  shape: BoxShape.circle,
                                  border: Border.all(color: Colors.white, width: 1.5),
                                ),
                                child: const Text('2', style: TextStyle(color: Colors.white, fontSize: 9, fontWeight: FontWeight.bold)),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                    const SizedBox(height: 20),
                    _buildChildSelector(),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _glow(double size, double alpha) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(shape: BoxShape.circle, color: Colors.white.withValues(alpha: alpha)),
      );

  /// Profile picture from My Profile with a ring; person icon when there is none. Tap: edit the profile
  Widget _profileAvatar() {
    final image = profile?.image;
    return GestureDetector(
      onTap: _openEditProfile,
      child: Container(
        padding: const EdgeInsets.all(2.5),
        decoration: const BoxDecoration(
          shape: BoxShape.circle,
          gradient: SweepGradient(colors: [Color(0xFFFFC044), Color(0xFFFF6B9A), Color(0xFF7CF0C8), Color(0xFFFFC044)]),
        ),
        child: CircleAvatar(
          radius: 23,
          backgroundColor: const Color(0xFFFFC044),
          foregroundImage: image != null ? MemoryImage(image) : null,
          // Shown while there is no image, or if the image cannot be decoded
          onForegroundImageError: image != null ? (_, __) {} : null,
          child: const Icon(Icons.person, color: Colors.white, size: 28),
        ),
      ),
    );
  }

  /// Children as stories (tap to switch); loading / error / none as a line of text
  Widget _buildChildSelector() {
    if (isLoadingChildren) {
      return _headerNote(
        const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2.2, color: Colors.white)),
        tr('جاري تحميل بيانات الأبناء...', 'Loading your children...'),
      );
    }
    if (childrenError != null) {
      return _headerNote(
        const Icon(Icons.error_outline_rounded, color: Color(0xFFFFB4B4)),
        childrenError!,
        action: TextButton(
          style: TextButton.styleFrom(foregroundColor: Colors.white),
          onPressed: _loadChildren,
          child: Text(tr('إعادة المحاولة', 'Try again')),
        ),
      );
    }
    if (children.isEmpty) {
      return _headerNote(
        const Icon(Icons.info_outline_rounded, color: Colors.white70),
        tr('لا يوجد أبناء مرتبطون بحسابك حالياً.', 'No children are linked to your account yet.'),
      );
    }

    final selected = selectedChild ?? children.first;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          height: 98,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            itemCount: children.length,
            separatorBuilder: (_, __) => const SizedBox(width: 14),
            itemBuilder: (context, i) => _childStory(children[i], children[i].id == selected.id),
          ),
        ),
        if (selected.classInfo.isNotEmpty)
          Container(
            margin: const EdgeInsets.only(top: 4),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: .14),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: Colors.white.withValues(alpha: .18)),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.school_outlined, color: Colors.white, size: 15),
                const SizedBox(width: 6),
                Text(selected.classInfo, style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
              ],
            ),
          ),
      ],
    );
  }

  /// One child: avatar in a ring (colourful when selected) and the name
  Widget _childStory(Child child, bool isSelected) {
    return GestureDetector(
      onTap: () => _selectChild(child),
      child: SizedBox(
        width: 74,
        child: Column(
          children: [
            AnimatedContainer(
              duration: const Duration(milliseconds: 250),
              padding: EdgeInsets.all(isSelected ? 3 : 2),
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: isSelected
                    ? const SweepGradient(colors: [Color(0xFFFFC044), Color(0xFFFF6B9A), Color(0xFF7C5CFF), Color(0xFF7CF0C8), Color(0xFFFFC044)])
                    : null,
                color: isSelected ? null : Colors.white.withValues(alpha: .35),
              ),
              child: Container(
                width: 60,
                height: 60,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isSelected ? Colors.white : Colors.white.withValues(alpha: .85),
                  border: Border.all(color: const Color(0xFF1046A8), width: 2),
                ),
                child: AnimatedScale(
                  scale: isSelected ? 1 : .85,
                  duration: const Duration(milliseconds: 250),
                  child: Text(child.avatar, style: const TextStyle(fontSize: 30)),
                ),
              ),
            ),
            const SizedBox(height: 6),
            Text(
              child.name,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              textAlign: TextAlign.center,
              style: TextStyle(
                color: Colors.white.withValues(alpha: isSelected ? 1 : .75),
                fontSize: 12,
                fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _headerNote(Widget leading, String text, {Widget? action}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .14),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Row(
        children: [
          leading,
          const SizedBox(width: 12),
          Expanded(child: Text(text, style: const TextStyle(color: Colors.white, fontSize: 13))),
          if (action != null) action,
        ],
      ),
    );
  }

  // ----- Today card: the lesson now (with progress) or the next one -----

  Widget _todayCard(ChildTimetable timetable) {
    final now = timetable.now;
    final slots = timetable.dayOf(timetable.today)?.slots ?? const <ScheduleSlot>[];
    final current = slots.where((s) => s.start <= now && now < s.end).firstOrNull;
    final next = slots.where((s) => s.start > now).firstOrNull;
    final done = slots.where((s) => s.end <= now).length;

    final String badge;
    final IconData badgeIcon;
    final String title;
    final String subtitle;
    String? footer;
    double? progress;
    Color color;

    if (slots.isEmpty) {
      // No lessons today: the next school day
      final upcoming = timetable.days.where((d) => d.slots.isNotEmpty).toList()
        ..sort((a, b) => ((a.dayOfWeek - timetable.today + 7) % 7).compareTo((b.dayOfWeek - timetable.today + 7) % 7));
      final nextDay = upcoming.where((d) => d.dayOfWeek != timetable.today).firstOrNull;
      badge = tr('اليوم', 'Today');
      badgeIcon = Icons.weekend_rounded;
      title = tr('لا توجد حصص اليوم', 'No lessons today');
      subtitle = nextDay == null
          ? tr('استمتعوا بيومكم!', 'Enjoy your day!')
          : tr('الدراسة ${dayNames[nextDay.dayOfWeek]} · ${lessonsCount(nextDay.slots.length)}',
              'Next school day: ${dayNames[nextDay.dayOfWeek]} · ${lessonsCount(nextDay.slots.length)}');
      color = const Color(0xFF0FA5A5);
    } else if (current != null) {
      badge = tr('الآن', 'Now');
      badgeIcon = Icons.play_arrow_rounded;
      title = current.title;
      subtitle = '${formatTime(current.start)} - ${formatTime(current.end)}'
          '${current.teacher.isNotEmpty ? ' · ${current.teacher}' : ''}';
      progress = ((now - current.start) / (current.end - current.start)).clamp(0, 1).toDouble();
      footer = tr('تنتهي ${formatIn(current.end - now)}', 'Ends ${formatIn(current.end - now)}');
      if (next != null) footer = '$footer  ·  ${tr('التالية', 'Next')}: ${next.title}';
      color = subjectColor(current);
    } else if (next != null) {
      badge = done == 0 ? tr('أول حصة', 'First lesson') : tr('التالية', 'Up next');
      badgeIcon = Icons.schedule_rounded;
      title = next.title;
      subtitle = '${formatTime(next.start)} - ${formatTime(next.end)}'
          '${next.teacher.isNotEmpty ? ' · ${next.teacher}' : ''}';
      footer = tr('تبدأ ${formatIn(next.start - now)}', 'Starts ${formatIn(next.start - now)}');
      color = subjectColor(next);
    } else {
      badge = tr('اليوم', 'Today');
      badgeIcon = Icons.celebration_rounded;
      title = tr('انتهى اليوم الدراسي 🎉', 'School day is over 🎉');
      subtitle = tr('${lessonsCount(slots.length)} اليوم', '${lessonsCount(slots.length)} today');
      progress = 1;
      color = const Color(0xFF20A66A);
    }

    final dark = Color.lerp(color, Colors.black, .35)!;
    return _Pressable(
      key: const ValueKey('today-card'),
      onTap: _openTimetable,
      child: Container(
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(26),
          gradient: LinearGradient(
            colors: [color, dark],
            begin: AlignmentDirectional.topStart,
            end: AlignmentDirectional.bottomEnd,
          ),
          boxShadow: [BoxShadow(color: color.withValues(alpha: .35), blurRadius: 18, offset: const Offset(0, 8))],
        ),
        child: Stack(
          clipBehavior: Clip.none,
          children: [
            PositionedDirectional(
              end: -10,
              top: -14,
              child: Icon(Icons.auto_stories_rounded, size: 96, color: Colors.white.withValues(alpha: .12)),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(color: Colors.white.withValues(alpha: .22), borderRadius: BorderRadius.circular(20)),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          if (current != null) const _LiveDot() else Icon(badgeIcon, color: Colors.white, size: 15),
                          const SizedBox(width: 6),
                          Text(badge, style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.bold)),
                        ],
                      ),
                    ),
                    const Spacer(),
                    if (slots.isNotEmpty)
                      Text('$done/${slots.length}',
                          style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                ),
                const SizedBox(height: 12),
                Text(title, style: const TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold)),
                const SizedBox(height: 3),
                Text(subtitle, style: TextStyle(color: Colors.white.withValues(alpha: .85), fontSize: 13)),
                if (progress != null) ...[
                  const SizedBox(height: 14),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: TweenAnimationBuilder<double>(
                      tween: Tween(begin: 0, end: progress),
                      duration: const Duration(milliseconds: 900),
                      curve: Curves.easeOutCubic,
                      builder: (_, value, __) => LinearProgressIndicator(
                        value: value,
                        minHeight: 7,
                        color: Colors.white,
                        backgroundColor: Colors.white.withValues(alpha: .25),
                      ),
                    ),
                  ),
                ],
                if (footer != null) ...[
                  const SizedBox(height: 10),
                  Text(footer, style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  // ----- Services as a bento grid; the big tiles show live data -----

  Widget _buildServices() {
    final timetable = _timetable;
    final todaySlots = timetable?.dayOf(timetable.today)?.slots.length;
    final timetableText = todaySlots == null
        ? tr('الأسبوع كاملاً', 'The whole week')
        : todaySlots == 0
            ? tr('لا حصص اليوم', 'No lessons today')
            : tr('${lessonsCount(todaySlots)} اليوم', '${lessonsCount(todaySlots)} today');

    final now = DateTime.now();
    final nextExam = (_exams ?? const <ChildExam>[]).where((e) => e.isUpcoming(now)).toList()
      ..sort((a, b) => a.date.compareTo(b.date));
    final examText = _exams == null
        ? tr('المواعيد والنتائج', 'Dates & results')
        : nextExam.isEmpty
            ? tr('لا اختبارات قادمة', 'No upcoming exams')
            : '${nextExam.first.subject} · ${_when(nextExam.first.date)}';

    final outstanding = (_fees ?? const <StudentFeeItem>[]).where((f) => f.isOutstanding).toList();
    final overdue = outstanding.where((f) => f.effectiveStatus == FeeState.overdue).length;
    final balance = outstanding.fold<double>(0, (sum, f) => sum + f.balance);
    final feeText = _fees == null
        ? tr('المستحقات والمدفوعات', 'Dues & payments')
        : outstanding.isEmpty
            ? tr('كل الرسوم مسددة ✓', 'All paid ✓')
            : tr('المتبقي ${_amount(balance, outstanding.first.currency)}', 'Left ${_amount(balance, outstanding.first.currency)}');

    final nextEvent = (_events ?? const <ChildEvent>[]).where((e) => e.isUpcoming(now)).toList()
      ..sort((a, b) => a.date.compareTo(b.date));
    final eventText = _events == null
        ? tr('الأنشطة والرحلات', 'Activities & trips')
        : nextEvent.isEmpty
            ? tr('لا فعاليات قادمة', 'No upcoming events')
            : '${nextEvent.first.title} · ${_when(nextEvent.first.date)}';

    const unit = 116.0;
    const gap = 12.0;
    final soon = [
      (Icons.menu_book_rounded, tr('الواجبات', 'Homework'), const Color(0xFF20A66A)),
      (Icons.how_to_reg_rounded, tr('الحضور والغياب', 'Attendance'), const Color(0xFF0FA5A5)),
      (Icons.local_hospital_rounded, tr('العيادة الصحية', 'School Clinic'), const Color(0xFFD93025)),
      (Icons.directions_bus_rounded, tr('الباص', 'School Bus'), const Color(0xFFB7791F)),
    ];

    return Column(
      children: [
        _Appear(
          order: 2,
          child: SizedBox(
            height: unit * 2 + gap,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(
                  child: _BentoTile(
                    icon: Icons.calendar_view_week_rounded,
                    label: tr('الجدول الدراسي', 'Timetable'),
                    detail: timetableText,
                    colors: const [Color(0xFF2F80ED), Color(0xFF1B4FC4)],
                    big: true,
                    onTap: _openTimetable,
                  ),
                ),
                const SizedBox(width: gap),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Expanded(
                        child: _BentoTile(
                          icon: Icons.fact_check_rounded,
                          label: tr('الاختبارات والدرجات', 'Exams & Grades'),
                          detail: examText,
                          colors: const [Color(0xFF9B6BFF), Color(0xFF6A3FD8)],
                          onTap: _openExams,
                        ),
                      ),
                      const SizedBox(height: gap),
                      Expanded(
                        child: _BentoTile(
                          icon: Icons.payments_rounded,
                          label: tr('الرسوم الدراسية', 'School Fees'),
                          detail: feeText,
                          colors: const [Color(0xFFFFA62B), Color(0xFFE5701A)],
                          badge: overdue > 0 ? '$overdue' : null,
                          onTap: _openFees,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: gap),
        _Appear(
          order: 3,
          child: SizedBox(
            height: unit - 16,
            child: _BentoTile(
              icon: Icons.celebration_rounded,
              label: tr('الفعاليات', 'Events'),
              detail: eventText,
              colors: const [Color(0xFFFF6B9A), Color(0xFFD9427A)],
              wide: true,
              onTap: _openEvents,
            ),
          ),
        ),
        const SizedBox(height: gap),
        _Appear(
          order: 4,
          child: Row(
            children: [
              for (var i = 0; i < soon.length; i++) ...[
                if (i > 0) const SizedBox(width: 10),
                Expanded(child: _soonTile(soon[i].$1, soon[i].$2, soon[i].$3)),
              ],
            ],
          ),
        ),
      ],
    );
  }

  /// A service that is not ready yet: small card with a "soon" label
  Widget _soonTile(IconData icon, String label, Color color) {
    return _Pressable(
      onTap: () => _showMessage(tr('خدمة "$label" ستتوفر قريباً.', 'The "$label" service is coming soon.')),
      child: Container(
        height: 104,
        padding: const EdgeInsets.fromLTRB(6, 10, 6, 8),
        decoration: BoxDecoration(
          color: context.palette.card,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: context.palette.line),
        ),
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(color: color.withValues(alpha: .12), shape: BoxShape.circle),
              child: Icon(icon, color: color.withValues(alpha: .8), size: 22),
            ),
            const SizedBox(height: 6),
            Expanded(
              child: Text(label,
                  textAlign: TextAlign.center,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 10.5, height: 1.2, fontWeight: FontWeight.w600, color: context.palette.ink)),
            ),
            Text(tr('قريباً', 'Soon'),
                style: TextStyle(fontSize: 9.5, fontWeight: FontWeight.bold, color: context.palette.faint)),
          ],
        ),
      ),
    );
  }

  /// "اليوم" / "غداً" / "بعد 3 أيام" / "12 ديسمبر"
  String _when(DateTime date) {
    final today = DateTime.now();
    final days = DateTime(date.year, date.month, date.day).difference(DateTime(today.year, today.month, today.day)).inDays;
    if (days <= 0) return tr('اليوم', 'today');
    if (days == 1) return tr('غداً', 'tomorrow');
    if (days <= 6) return tr('بعد $days أيام', 'in $days days');
    return '${date.day} ${monthNames[date.month - 1]}';
  }

  /// 1250.5 EGP -> "1,251 ج.م" / "1,251 EGP"
  String _amount(double value, String currency) {
    final digits = value.round().toString().replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');
    final symbol = switch (currency.toUpperCase()) {
      'EGP' => tr('ج.م', 'EGP'),
      'SAR' => tr('ر.س', 'SAR'),
      'AED' => tr('د.إ', 'AED'),
      'KWD' => tr('د.ك', 'KWD'),
      final other => other,
    };
    return '$digits $symbol';
  }

  /// Timetable page of the selected child
  void _openTimetable() => _openForChild((child) => TimetableScreen(
        child: child,
        loadTimetable: widget.loadTimetable,
        onSessionExpired: _logout,
      ));

  /// Fees page of the selected child
  /// Exams page of the selected child
  /// Events page of the selected child
  void _openEvents() => _openForChild((child) => EventsScreen(
        child: child,
        loadEvents: widget.loadEvents,
        onSessionExpired: _logout,
      ));

  void _openExams() => _openForChild((child) => ExamsScreen(
        child: child,
        loadExams: widget.loadExams,
        onSessionExpired: _logout,
      ));

  void _openFees() => _openForChild((child) => FeesScreen(
        child: child,
        loadFees: widget.loadFees,
        onSessionExpired: _logout,
      ));

  /// Opens a page of the child selected in the header (message when there is none yet)
  void _openForChild(Widget Function(Child child) page) {
    final child = selectedChild;
    if (child == null) {
      _showMessage(isLoadingChildren
          ? tr('جاري تحميل بيانات الأبناء...', 'Loading your children...')
          : tr('لا يوجد أبناء مرتبطون بحسابك.', 'No children are linked to your account.'));
      return;
    }
    _nav.push(MaterialPageRoute(builder: (_) => page(child)));
  }

  /// The important announcement: a glowing strip with a megaphone
  Widget _buildAnnouncement() {
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 14, 14, 14),
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.circular(22),
        border: Border.all(color: const Color(0xFFFFC044).withValues(alpha: .55)),
        boxShadow: [BoxShadow(color: const Color(0xFFFFB21A).withValues(alpha: .14), blurRadius: 16, offset: const Offset(0, 6))],
      ),
      child: Row(
        children: [
          Container(
            width: 46,
            height: 46,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              gradient: LinearGradient(colors: [Color(0xFFFFC044), Color(0xFFFF8A3D)]),
            ),
            child: const Icon(Icons.campaign_rounded, color: Colors.white, size: 26),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('إعلان هام', 'Important notice'),
                    style: TextStyle(color: context.palette.ink, fontWeight: FontWeight.bold, fontSize: 15.5)),
                const SizedBox(height: 3),
                Text(tr('موعد الرحلة المدرسية القادمة يوم الخميس القادم.', 'The next school trip is next Thursday.'),
                    style: TextStyle(color: context.palette.muted, fontSize: 12.5)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
            decoration: BoxDecoration(
              color: AppTheme.blue.withValues(alpha: .1),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Text(tr('عرض الكل', 'View all'),
                style: const TextStyle(color: AppTheme.blue, fontWeight: FontWeight.bold, fontSize: 11)),
          ),
        ],
      ),
    );
  }

  Widget _sectionTitle(String title) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: Row(
          children: [
            Container(
              width: 5,
              height: 20,
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(4),
                gradient: const LinearGradient(
                  colors: [Color(0xFF2F80ED), Color(0xFF7C5CFF)],
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                ),
              ),
            ),
            const SizedBox(width: 8),
            Text(title, style: TextStyle(fontSize: 19, fontWeight: FontWeight.bold, color: context.palette.ink)),
          ],
        ),
      );

  /// Floating rounded bar; the selected item becomes a coloured pill
  Widget _bottomNavigation() {
    final items = [
      (Icons.home_rounded, tr('الرئيسية', 'Home')),
      (Icons.notifications_rounded, tr('الإشعارات', 'Notifications')),
      (Icons.chat_bubble_rounded, tr('المحادثات', 'Chats')),
      (Icons.menu_rounded, tr('المزيد', 'More')),
    ];

    void select(int index) {
      if (index == 3) {
        _showMoreMenu();
        return;
      }
      // Home from any page: back to the home page
      if (index == 0) _nav.popUntil((route) => route.isFirst);
      setState(() => selectedTab = index);
      if (index != 0) {
        _showMessage(tr('صفحة ${items[index].$2} سيتم تنفيذها في المرحلة التالية.',
            'The ${items[index].$2} page is coming in the next phase.'));
      }
    }

    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(14, 0, 14, 10),
        child: Container(
          height: 68,
          padding: const EdgeInsets.symmetric(horizontal: 6),
          decoration: BoxDecoration(
            color: context.palette.card,
            borderRadius: BorderRadius.circular(26),
            border: Border.all(color: context.palette.line),
            boxShadow: const [BoxShadow(color: Color(0x260B3470), blurRadius: 24, offset: Offset(0, 8))],
          ),
          child: Row(
            children: [
              for (var i = 0; i < items.length; i++)
                Expanded(
                  child: InkWell(
                    borderRadius: BorderRadius.circular(20),
                    onTap: () => select(i),
                    child: Center(
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 250),
                        curve: Curves.easeOutCubic,
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: i == selectedTab ? AppTheme.blue.withValues(alpha: .12) : Colors.transparent,
                          borderRadius: BorderRadius.circular(18),
                        ),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(items[i].$1,
                                size: 24, color: i == selectedTab ? AppTheme.blue : context.palette.faint),
                            const SizedBox(height: 2),
                            Text(items[i].$2,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: TextStyle(
                                  fontSize: 10.5,
                                  fontWeight: i == selectedTab ? FontWeight.bold : FontWeight.w500,
                                  color: i == selectedTab ? AppTheme.blue : context.palette.muted,
                                )),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  /// Email under the name in the menu; the login name when there is no real email (placeholder or empty)
  String _menuSubtitle(AuthSession session) {
    final p = profile;
    if (p != null) {
      return p.email.isNotEmpty && !p.hasPlaceholderEmail
          ? p.email
          : p.loginName;
    }
    final email = session.email ?? '';
    return email.isNotEmpty && !email.toLowerCase().endsWith('@parents.local')
        ? email
        : session.loginName;
  }

  /// "More" tab: account info and logout
  void _showMoreMenu() {
    final session = AuthService.instance.session;
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      isScrollControlled: true,
      builder: (sheetContext) => SafeArea(
        child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (session != null)
              ListTile(
                leading: const Icon(Icons.account_circle_rounded,
                    color: AppTheme.blue),
                // Latest profile (refreshed after an edit), else what was saved at login
                title: Text(profile?.displayName ?? session.displayName,
                    style: const TextStyle(fontWeight: FontWeight.bold)),
                subtitle: Text(_menuSubtitle(session)),
              ),
            ListTile(
              leading: const Icon(Icons.manage_accounts_rounded,
                  color: AppTheme.blue),
              title: Text(tr('تعديل الملف الشخصي', 'Edit profile')),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _openEditProfile();
              },
            ),
            ListTile(
              leading: const Icon(Icons.school_rounded, color: AppTheme.blue),
              title: Text(tr('عن المدرسة', 'About the school')),
              subtitle: Text(tr('بيانات المدرسة وألبوم الصور', 'School details and photo album')),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _nav.push(MaterialPageRoute(
                  builder: (_) =>
                      SchoolInfoScreen(loadSchoolInfo: widget.loadSchoolInfo),
                ));
              },
            ),
            // Light / dark / auto (follows the phone); saved on the device
            ValueListenableBuilder<ThemeMode>(
              valueListenable: ThemeController.instance,
              builder: (_, mode, __) => _settingRow(
                Icons.palette_rounded,
                tr('المظهر', 'Appearance'),
                SegmentedButton<ThemeMode>(
                  key: const ValueKey('theme-mode'),
                  showSelectedIcon: false,
                  segments: [
                    ButtonSegment(
                        value: ThemeMode.light,
                        icon: const Icon(Icons.light_mode_rounded),
                        label: Text(tr('فاتح', 'Light'))),
                    ButtonSegment(
                        value: ThemeMode.dark,
                        icon: const Icon(Icons.dark_mode_rounded),
                        label: Text(tr('داكن', 'Dark'))),
                    ButtonSegment(
                        value: ThemeMode.system,
                        icon: const Icon(Icons.brightness_auto_rounded),
                        label: Text(tr('تلقائي', 'Auto'))),
                  ],
                  selected: {mode},
                  onSelectionChanged: (s) =>
                      ThemeController.instance.setMode(s.first),
                ),
              ),
            ),
            // Arabic / English; saved on the device
            ValueListenableBuilder<Locale>(
              valueListenable: AppLanguage.instance,
              builder: (_, locale, __) => _settingRow(
                Icons.translate_rounded,
                tr('اللغة', 'Language'),
                const LanguageSwitch(key: ValueKey('app-language')),
              ),
            ),
            ListTile(
              leading:
                  const Icon(Icons.logout_rounded, color: Color(0xFFD93025)),
              title: Text(tr('تسجيل الخروج', 'Sign out'),
                  style: TextStyle(color: Color(0xFFD93025))),
              onTap: () async {
                Navigator.of(sheetContext).pop();
                await _logout();
              },
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
      ),
    );
  }

  /// A menu setting: icon and title, the choice under them
  Widget _settingRow(IconData icon, String title, Widget choice) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 6, 16, 6),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: AppTheme.blue),
              const SizedBox(width: 16),
              Text(title, style: const TextStyle(fontSize: 16)),
            ],
          ),
          const SizedBox(height: 10),
          SizedBox(width: double.infinity, child: choice),
        ],
      ),
    );
  }

  Future<void> _logout() async {
    await AuthService.instance.logout();
    if (!mounted) return;
    Navigator.of(context)
        .pushNamedAndRemoveUntil(AppRoutes.login, (_) => false);
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }
}

/// Bento tile: gradient card with an icon, the service name and a live line under it.
/// [big]: tall tile (icon on top, text at the bottom); [wide]: full-width row.
class _BentoTile extends StatelessWidget {
  const _BentoTile({
    required this.icon,
    required this.label,
    required this.detail,
    required this.colors,
    required this.onTap,
    this.big = false,
    this.wide = false,
    this.badge,
  });

  final IconData icon;
  final String label;
  final String detail;
  final List<Color> colors;
  final VoidCallback onTap;
  final bool big;
  final bool wide;

  /// Small red count in the corner (e.g. overdue fees)
  final String? badge;

  @override
  Widget build(BuildContext context) {
    final iconChip = Container(
      padding: const EdgeInsets.all(9),
      decoration: BoxDecoration(color: Colors.white.withValues(alpha: .22), borderRadius: BorderRadius.circular(14)),
      child: Icon(icon, color: Colors.white, size: big ? 28 : 22),
    );
    final texts = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            maxLines: big ? 2 : 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: Colors.white, fontSize: big ? 18 : 14, height: 1.2, fontWeight: FontWeight.bold)),
        const SizedBox(height: 3),
        Text(detail,
            maxLines: big ? 2 : 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(color: Colors.white.withValues(alpha: .88), fontSize: big ? 13 : 11.5)),
      ],
    );

    return _Pressable(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(24),
          gradient: LinearGradient(colors: colors, begin: AlignmentDirectional.topStart, end: AlignmentDirectional.bottomEnd),
          boxShadow: [BoxShadow(color: colors.last.withValues(alpha: .30), blurRadius: 14, offset: const Offset(0, 6))],
        ),
        clipBehavior: Clip.antiAlias,
        child: Stack(
          children: [
            // Big see-through icon in the corner
            PositionedDirectional(
              end: big ? -22 : -14,
              bottom: big ? -18 : -16,
              child: Transform.rotate(
                angle: -.2,
                child: Icon(icon, size: big ? 130 : 78, color: Colors.white.withValues(alpha: .13)),
              ),
            ),
            Padding(
              padding: EdgeInsets.all(big ? 16 : 12),
              child: wide
                  ? Row(
                      children: [
                        iconChip,
                        const SizedBox(width: 12),
                        Expanded(child: texts),
                        // Mirrors itself in Arabic (points left)
                        const Icon(Icons.arrow_forward_rounded, color: Colors.white70, size: 20),
                      ],
                    )
                  : Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        iconChip,
                        const Spacer(),
                        texts,
                      ],
                    ),
            ),
            if (badge != null)
              PositionedDirectional(
                top: 10,
                end: 10,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
                  decoration: BoxDecoration(
                    color: const Color(0xFFD93025),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: Colors.white, width: 1.5),
                  ),
                  child: Text(badge!, style: const TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// Shrinks a little while pressed
class _Pressable extends StatefulWidget {
  const _Pressable({super.key, required this.child, required this.onTap});

  final Widget child;
  final VoidCallback onTap;

  @override
  State<_Pressable> createState() => _PressableState();
}

class _PressableState extends State<_Pressable> {
  bool _down = false;

  void _set(bool down) {
    if (_down != down) setState(() => _down = down);
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTapDown: (_) => _set(true),
      onTapCancel: () => _set(false),
      onTapUp: (_) => _set(false),
      onTap: widget.onTap,
      child: AnimatedScale(
        scale: _down ? .96 : 1,
        duration: const Duration(milliseconds: 120),
        child: widget.child,
      ),
    );
  }
}

/// Fades and slides in when Home opens, one block after the other ([order])
class _Appear extends StatelessWidget {
  const _Appear({required this.order, required this.child});

  final int order;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0, end: 1),
      duration: Duration(milliseconds: 380 + order * 90),
      curve: Curves.easeOutCubic,
      builder: (context, t, child) => Opacity(
        opacity: t,
        child: Transform.translate(offset: Offset(0, (1 - t) * 18), child: child),
      ),
      child: child,
    );
  }
}

/// White dot with a soft halo for "now"
class _LiveDot extends StatelessWidget {
  const _LiveDot();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 9,
      height: 9,
      decoration: BoxDecoration(
        color: Colors.white,
        shape: BoxShape.circle,
        boxShadow: [BoxShadow(color: Colors.white.withValues(alpha: .7), blurRadius: 6, spreadRadius: 1.5)],
      ),
    );
  }
}
