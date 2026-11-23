import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/auth/auth_service.dart';
import '../../core/i18n/app_language.dart';
import '../../core/i18n/language_switch.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/theme_controller.dart';
import '../attendance/attendance_screens.dart';
import '../chat/chat_screens.dart';
import '../evaluation/evaluation_screens.dart';
import '../notifications/notifications_screen.dart';
import '../notifications/notifications_service.dart';
import '../profile/edit_profile_screen.dart';
import '../profile/profile_service.dart';
import '../school/school_info_screen.dart';
import '../school/school_info_service.dart';
import '../homework/homework_screens.dart';
import '../classes/classes_screens.dart';
import '../classes/classes_service.dart';
import '../events/event_format.dart' as ev;
import '../events/events_screen.dart';
import '../parents/parents_screen.dart';
import '../parents/parents_service.dart';
import '../events/events_service.dart';
import '../exams/exam_screens.dart';
import '../timetable/teacher_timetable_screen.dart';
import '../timetable/teacher_timetable_service.dart';

/// Teacher home: greeting, the important notice and the teacher services; the bottom bar stays on every page.
class HomeScreen extends StatefulWidget {
  const HomeScreen({
    super.key,
    this.chatApi = const ChatApi(),
    this.loadNotifications,
    this.chatRefreshEvery = const Duration(seconds: 15),
    this.loadProfile,
    this.loadSchoolInfo,
    this.homeworkApi = const HomeworkApi(),
    this.examApi = const ExamApi(),
    this.attendanceApi,
    this.evaluationApi,
    this.loadEvents,
    this.loadMyClasses,
    this.loadClassStudents,
    this.loadParents,
    this.loadAcademicYears,
    this.loadTimetable,
  });

  /// Loads the teacher's timetable; defaults to the API (tests pass their own)
  final Future<TeacherTimetable> Function()? loadTimetable;

  /// Loads the academic years (year picker); defaults to the API
  final Future<List<AcademicYear>> Function()? loadAcademicYears;

  /// Homework API calls; defaults to the API (tests pass their own)
  final HomeworkApi homeworkApi;

  /// Exam / score API calls; defaults to the API (tests pass their own)
  final ExamApi examApi;

  /// Lesson attendance API calls; defaults to the API with [loadTimetable] / [loadClassStudents] (tests pass their own)
  final AttendanceApi? attendanceApi;

  /// Student evaluation / parent request API calls; defaults to the API with [loadTimetable] / [loadMyClasses] /
  /// [loadClassStudents] (tests pass their own)
  final EvaluationApi? evaluationApi;

  /// Loads the teacher's events (for everyone / for teachers); defaults to the API
  final Future<List<SchoolEvent>> Function()? loadEvents;

  /// Loads the teacher's classes and a class's students; default to the API
  final Future<List<MyClass>> Function()? loadMyClasses;
  final Future<List<ClassStudent>> Function(String sectionId)? loadClassStudents;

  /// Loads the parents of the teacher's students; defaults to the API
  final Future<List<TeacherParent>> Function()? loadParents;

  /// Loads the signed-in user's profile (name + picture); defaults to the API (tests pass their own)
  final Future<MyProfile?> Function()? loadProfile;

  /// Loads the school information (logo, About the school page); defaults to the API
  final Future<SchoolInfo?> Function()? loadSchoolInfo;

  /// Chat API calls; defaults to the API (tests pass their own)
  final ChatApi chatApi;

  /// Loads my notifications; defaults to the API (tests pass their own)
  final Future<List<AppNotice>> Function()? loadNotifications;

  /// How often the chats reload while open (null: never; tests)
  final Duration? chatRefreshEvery;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int selectedTab = 0;
  MyProfile? profile;
  SchoolInfo? schoolInfo;

  /// Pages opened from Home open in this navigator, so the bottom bar stays pinned under every page
  final _navKey = GlobalKey<NavigatorState>();

  NavigatorState get _nav => _navKey.currentState!;

  @override
  void initState() {
    super.initState();
    _loadProfile();
    _loadSchoolInfo();
    _loadEvents();
    // Academic years for the picker (homework is listed / created in the chosen year)
    AcademicYearController.instance.ensureLoaded(fetch: widget.loadAcademicYears);
    // Another year: its events
    AcademicYearController.instance.addListener(_loadEvents);
  }

  @override
  void dispose() {
    AcademicYearController.instance.removeListener(_loadEvents);
    super.dispose();
  }

  /// Events for everyone / for teachers (chosen academic year), for the important notice
  List<SchoolEvent>? _events;

  Future<void> _loadEvents() async {
    try {
      final result = await (widget.loadEvents ?? EventsService.instance.getMyEvents)();
      if (mounted) setState(() => _events = result);
    } catch (_) {
      // No notice then; the events page reports the error
      if (mounted) setState(() => _events = null);
    }
  }

  /// The next event that is not over yet (null while loading / when there is none)
  SchoolEvent? get _nextEvent {
    final now = DateTime.now();
    final upcoming = (_events ?? const <SchoolEvent>[]).where((e) => e.isUpcoming(now)).toList()
      ..sort((a, b) {
        final d = a.date.compareTo(b.date);
        return d != 0 ? d : a.start.compareTo(b.start);
      });
    return upcoming.isEmpty ? null : upcoming.first;
  }

  /// "اليوم" / "غداً" / "بعد 3 أيام" / "25 نوفمبر"
  String _eventWhen(DateTime date) {
    final today = DateTime.now();
    final days = DateTime(date.year, date.month, date.day).difference(DateTime(today.year, today.month, today.day)).inDays;
    if (days <= 0) return tr('اليوم', 'today');
    if (days == 1) return tr('غداً', 'tomorrow');
    if (days <= 6) return tr('بعد $days أيام', 'in $days days');
    return '${date.day} ${ev.monthNames[date.month - 1]}';
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
    } catch (_) {
      // Keep the login name
    }
  }

  /// School logo for the header; on failure (or no logo) the header keeps the school icon
  Future<void> _loadSchoolInfo() async {
    try {
      final loader = widget.loadSchoolInfo ?? SchoolInfoService.instance.getSchoolInfo;
      final result = await loader();
      if (mounted) setState(() => schoolInfo = result);
    } catch (_) {
      // Keep the icon
    }
  }

  Future<void> _refresh() => Future.wait([_loadProfile(), _loadSchoolInfo(), _loadEvents()]);

  // ----- Navigation -----

  void _openTimetable() {
    _nav.push(MaterialPageRoute(
      builder: (_) => TeacherTimetableScreen(loadTimetable: widget.loadTimetable, onSessionExpired: _logout),
    ));
  }

  void _openHomework() {
    _nav.push(MaterialPageRoute(
      builder: (_) => HomeworkListScreen(api: widget.homeworkApi, onSessionExpired: _logout),
    ));
  }

  void _openClasses() {
    _nav.push(MaterialPageRoute(
      builder: (_) => MyClassesScreen(
        loadClasses: widget.loadMyClasses,
        loadStudents: widget.loadClassStudents,
        onSessionExpired: _logout,
      ),
    ));
  }

  void _openParents() {
    _nav.push(MaterialPageRoute(
      builder: (_) => ParentsScreen(
        loadParents: widget.loadParents,
        onSessionExpired: _logout,
        chatApi: widget.chatApi,
        chatRefreshEvery: widget.chatRefreshEvery == null ? null : const Duration(seconds: 5),
      ),
    ));
  }

  void _openEvents() {
    _nav.push(MaterialPageRoute(
      builder: (_) => EventsScreen(loadEvents: widget.loadEvents, onSessionExpired: _logout),
    ));
  }

  void _openAttendance() {
    final api = widget.attendanceApi ??
        AttendanceApi(loadTimetable: widget.loadTimetable, loadStudents: widget.loadClassStudents);
    _nav.push(MaterialPageRoute(
      builder: (_) => AttendanceLessonsScreen(api: api, onSessionExpired: _logout),
    ));
  }

  void _openEvaluations() {
    final api = widget.evaluationApi ??
        EvaluationApi(
          loadTimetable: widget.loadTimetable,
          loadClasses: widget.loadMyClasses,
          loadStudents: widget.loadClassStudents,
        );
    _nav.push(MaterialPageRoute(
      builder: (_) => EvaluationsScreen(api: api, onSessionExpired: _logout),
    ));
  }

  void _openExams() {
    _nav.push(MaterialPageRoute(
      builder: (_) => ExamListScreen(api: widget.examApi, onSessionExpired: _logout),
    ));
  }

  void _openSchoolInfo() {
    _nav.push(MaterialPageRoute(builder: (_) => SchoolInfoScreen(loadSchoolInfo: widget.loadSchoolInfo)));
  }

  /// Edit profile; the header is refreshed when something was saved
  Future<void> _openEditProfile() async {
    var current = profile;
    if (current == null) {
      await _loadProfile();
      current = profile;
    }
    if (!mounted) return;
    if (current == null) {
      _showMessage(tr('تعذر تحميل الملف الشخصي، حاول مرة أخرى.', 'Could not load your profile. Please try again.'));
      return;
    }
    final saved = await _nav.push<bool>(MaterialPageRoute(builder: (_) => EditProfileScreen(profile: current!)));
    if (!mounted) return;
    if (AuthService.instance.session?.isExpired ?? true) {
      await _logout();
      return;
    }
    if (saved == true) {
      setState(() => selectedTab = 0);
      await _refresh();
    }
  }

  Future<void> _logout() async {
    await AuthService.instance.logout();
    await AcademicYearController.instance.reset();
    if (!mounted) return;
    // The app navigator (not the one inside Home): back to the login page
    Navigator.of(context).pushNamedAndRemoveUntil(AppRoutes.login, (_) => false);
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  void _comingSoon(String label) =>
      _showMessage(tr('خدمة "$label" ستتوفر قريباً.', 'The "$label" service is coming soon.'));

  // ----- Page -----

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
                  // The next event as the important notice (none: hidden)
                  if (_nextEvent != null) ...[
                    _Appear(order: 0, child: _buildAnnouncement(_nextEvent!)),
                    const SizedBox(height: 22),
                  ],
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

  // ----- Header: school logo, greeting, profile picture, role and today's date -----

  Widget _buildHeader() {
    final now = DateTime.now();
    final greeting = now.hour < 12 ? tr('صباح الخير ☀️', 'Good morning ☀️') : tr('مساء الخير 🌙', 'Good evening 🌙');
    return ClipRRect(
      borderRadius: const BorderRadius.vertical(bottom: Radius.circular(36)),
      child: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            // Teal / indigo: the teacher app's own colours
            colors: [Color(0xFF13A89E), Color(0xFF1B5FAA), Color(0xFF3B1F94)],
            begin: AlignmentDirectional.topStart,
            end: AlignmentDirectional.bottomEnd,
          ),
        ),
        child: Stack(
          children: [
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
                                profile?.displayName ?? AuthService.instance.session?.displayName ?? tr('المعلم', 'Teacher'),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: const TextStyle(color: Colors.white, fontSize: 21, fontWeight: FontWeight.bold),
                              ),
                            ],
                          ),
                        ),
                        _profileAvatar(),
                      ],
                    ),
                    const SizedBox(height: 18),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        // Job level from the teacher record (read only), else just "Teacher"
                        _headerChip(
                          Icons.co_present_rounded,
                          (profile?.jobLevel.isNotEmpty ?? false) ? profile!.jobLevel : tr('معلم', 'Teacher'),
                        ),
                        _headerChip(Icons.calendar_today_rounded, _dateText(now)),
                        // Academic year (tap to change)
                        const AcademicYearChip(),
                        if (schoolInfo != null && schoolInfo!.name.isNotEmpty)
                          _headerChip(Icons.apartment_rounded, schoolInfo!.name),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// "الخميس 20 نوفمبر" / "Thursday 20 November"
  static String _dateText(DateTime d) {
    const arDays = ['الاثنين', 'الثلاثاء', 'الأربعاء', 'الخميس', 'الجمعة', 'السبت', 'الأحد'];
    const enDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    const arMonths = ['يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو', 'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'];
    const enMonths = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    final en = AppLanguage.instance.isEnglish;
    return '${(en ? enDays : arDays)[d.weekday - 1]} ${d.day} ${(en ? enMonths : arMonths)[d.month - 1]}';
  }

  Widget _headerChip(IconData icon, String text) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: .14),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: Colors.white.withValues(alpha: .18)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, color: Colors.white, size: 15),
          const SizedBox(width: 6),
          Text(text, style: const TextStyle(color: Colors.white, fontSize: 12.5, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  Widget _glow(double size, double alpha) => Container(
        width: size,
        height: size,
        decoration: BoxDecoration(shape: BoxShape.circle, color: Colors.white.withValues(alpha: alpha)),
      );

  /// Logo from the school info (white circle), else the school icon; tap: About the school
  Widget _schoolLogo() {
    final logo = schoolInfo?.logo;
    return Tooltip(
      message: schoolInfo?.name ?? tr('عن المدرسة', 'About the school'),
      child: GestureDetector(
        key: const ValueKey('school-logo'),
        onTap: _openSchoolInfo,
        child: logo == null
            ? const CircleAvatar(
                radius: 25,
                backgroundColor: Colors.white24,
                child: Icon(Icons.school_rounded, color: Colors.white, size: 28),
              )
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

  /// Profile picture with a ring; person icon when there is none. Tap: edit the profile
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
          backgroundColor: const Color(0xFF13A89E),
          foregroundImage: image != null ? MemoryImage(image) : null,
          onForegroundImageError: image != null ? (_, __) {} : null,
          child: const Icon(Icons.person, color: Colors.white, size: 28),
        ),
      ),
    );
  }

  // ----- Notice and services -----

  /// The next upcoming event as the important notice (hidden when there is none); tap it or "View all" for the events
  Widget _buildAnnouncement(SchoolEvent event) {
    final palette = context.palette;
    return Material(
      color: palette.card,
      borderRadius: BorderRadius.circular(22),
      child: InkWell(
        key: const ValueKey('announcement'),
        borderRadius: BorderRadius.circular(22),
        onTap: _openEvents,
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
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
                        style: TextStyle(color: palette.ink, fontWeight: FontWeight.bold, fontSize: 15.5)),
                    const SizedBox(height: 3),
                    Text(event.title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(color: palette.ink, fontSize: 13, fontWeight: FontWeight.w600)),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        Icon(Icons.event_rounded, size: 13, color: palette.muted),
                        const SizedBox(width: 3),
                        Flexible(
                          child: Text('${_eventWhen(event.date)} · ${ev.formatTime(event.start)}',
                              maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: palette.muted, fontSize: 12)),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              InkWell(
                key: const ValueKey('announcement-view-all'),
                borderRadius: BorderRadius.circular(20),
                onTap: _openEvents,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                  decoration: BoxDecoration(color: AppTheme.blue.withValues(alpha: .1), borderRadius: BorderRadius.circular(20)),
                  child: Text(tr('عرض الكل', 'View all'),
                      style: const TextStyle(color: AppTheme.blue, fontWeight: FontWeight.bold, fontSize: 11)),
                ),
              ),
            ],
          ),
        ),
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
                  colors: [Color(0xFF13A89E), Color(0xFF7C5CFF)],
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

  /// Bento grid of the teacher services (they open as the teacher pages are added)
  Widget _buildServices() {
    const unit = 116.0;
    const gap = 12.0;
    final small = [
      (Icons.celebration_rounded, tr('الفعاليات', 'Events'), const Color(0xFFE0457B)),
      (Icons.groups_rounded, tr('فصولي', 'My classes'), const Color(0xFF0FA5A5)),
      (Icons.rate_review_rounded, tr('تقييم الطلاب', 'Evaluations'), const Color(0xFF4C5FD5)),
      (Icons.chat_rounded, tr('أولياء الأمور', 'Parents'), const Color(0xFFB7791F)),
    ];

    return Column(
      children: [
        _Appear(
          order: 1,
          child: SizedBox(
            height: unit * 2 + gap,
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Expanded(
                  // My timetable (for the chosen academic year / semester)
                  child: _BentoTile(
                    icon: Icons.calendar_view_week_rounded,
                    label: tr('جدول حصصي', 'My timetable'),
                    detail: tr('حصصك اليوم وخلال الأسبوع', 'Your lessons today and this week'),
                    colors: const [Color(0xFF13A89E), Color(0xFF0E7C8C)],
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
                        // Lesson attendance of the teacher's classes
                        child: _BentoTile(
                          icon: Icons.how_to_reg_rounded,
                          label: tr('الحضور والغياب', 'Attendance'),
                          detail: tr('سجّل حضور طلاب حصصك', 'Take attendance in your lessons'),
                          colors: const [Color(0xFF2F80ED), Color(0xFF1B4FC4)],
                          onTap: _openAttendance,
                        ),
                      ),
                      const SizedBox(height: gap),
                      Expanded(
                        // Quizzes / monthly exams and the students' scores
                        child: _BentoTile(
                          icon: Icons.fact_check_rounded,
                          label: tr('الاختبارات والدرجات', 'Exams & scores'),
                          detail: tr('أضف اختباراً وارصد الدرجات', 'Add an exam, enter scores'),
                          colors: const [Color(0xFF9B6BFF), Color(0xFF6A3FD8)],
                          onTap: _openExams,
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
          order: 2,
          child: SizedBox(
            height: unit - 16,
            // Homework: list, add and publish to classes
            child: _BentoTile(
              icon: Icons.menu_book_rounded,
              label: tr('الواجبات', 'Homework'),
              detail: tr('أضف الواجبات وانشرها لفصولك', 'Add homework and publish it to your classes'),
              colors: const [Color(0xFF2BC48A), Color(0xFF14916A)],
              wide: true,
              onTap: _openHomework,
            ),
          ),
        ),
        const SizedBox(height: gap),
        _Appear(
          order: 3,
          child: Row(
            children: [
              for (var i = 0; i < small.length; i++) ...[
                if (i > 0) const SizedBox(width: 10),
                // Events, my classes and parents are ready; the others say "soon"
                Expanded(
                  child: _soonTile(small[i].$1, small[i].$2, small[i].$3,
                      onTap: switch (i) { 0 => _openEvents, 1 => _openClasses, 2 => _openEvaluations, 3 => _openParents, _ => null }),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }

  /// A service that is not ready yet: small card with a "soon" label
  Widget _soonTile(IconData icon, String label, Color color, {VoidCallback? onTap}) {
    return _Pressable(
      onTap: onTap ?? () => _comingSoon(label),
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
            if (onTap == null)
              Text(tr('قريباً', 'Soon'), style: TextStyle(fontSize: 9.5, fontWeight: FontWeight.bold, color: context.palette.faint)),
          ],
        ),
      ),
    );
  }

  // ----- Bottom bar and the More menu -----

  /// Notifications for everyone / for me (chosen academic year), above the home page; the bar stays
  Future<void> _openNotifications() async {
    if (selectedTab == 1) return;
    _nav.popUntil((route) => route.isFirst);
    setState(() => selectedTab = 1);
    await _nav.push(MaterialPageRoute(
      builder: (_) => NotificationsScreen(
        loadNotices: widget.loadNotifications ?? NotificationsService.instance.getMine,
        targetedLabel: tr('لك', 'For you'),
        onSessionExpired: _logout,
      ),
    ));
    if (mounted && selectedTab == 1) setState(() => selectedTab = 0);
  }

  /// Chats (teachers <-> parents of their students), above the home page; the bar stays
  Future<void> _openChats() async {
    if (selectedTab == 2) return;
    _nav.popUntil((route) => route.isFirst);
    setState(() => selectedTab = 2);
    await _nav.push(MaterialPageRoute(
      builder: (_) => ChatListScreen(
        api: widget.chatApi,
        onSessionExpired: _logout,
        refreshEvery: widget.chatRefreshEvery,
        newChatHint: tr('راسل أولياء أمور طلابك.', 'Message the parents of your students.'),
      ),
    ));
    if (mounted && selectedTab == 2) setState(() => selectedTab = 0);
  }

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
      if (index == 1) {
        _openNotifications();
        return;
      }
      if (index == 2) {
        _openChats();
        return;
      }
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
                            Icon(items[i].$1, size: 24, color: i == selectedTab ? AppTheme.blue : context.palette.faint),
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

  /// "More": account, edit profile, about the school, appearance, language, sign out
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
                  leading: const Icon(Icons.account_circle_rounded, color: AppTheme.blue),
                  title: Text(profile?.displayName ?? session.displayName,
                      style: const TextStyle(fontWeight: FontWeight.bold)),
                  subtitle: Text((session.email ?? '').isNotEmpty && !session.email!.toLowerCase().endsWith('.local')
                      ? session.email!
                      : session.loginName),
                ),
              ListTile(
                leading: const Icon(Icons.manage_accounts_rounded, color: AppTheme.blue),
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
                  _openSchoolInfo();
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
                    onSelectionChanged: (s) => ThemeController.instance.setMode(s.first),
                  ),
                ),
              ),
              // Academic year (default: the current one); saved on the device
              _settingRow(
                Icons.date_range_rounded,
                tr('السنة الدراسية', 'Academic year'),
                const AcademicYearPicker(),
              ),
              // Arabic / English; saved on the device
              ValueListenableBuilder<Locale>(
                valueListenable: AppLanguage.instance,
                builder: (_, __, ___) => _settingRow(
                  Icons.translate_rounded,
                  tr('اللغة', 'Language'),
                  const LanguageSwitch(key: ValueKey('app-language')),
                ),
              ),
              ListTile(
                leading: const Icon(Icons.logout_rounded, color: Color(0xFFD93025)),
                title: Text(tr('تسجيل الخروج', 'Sign out'), style: const TextStyle(color: Color(0xFFD93025))),
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
  });

  final IconData icon;
  final String label;
  final String detail;
  final List<Color> colors;
  final VoidCallback onTap;
  final bool big;
  final bool wide;

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
          ],
        ),
      ),
    );
  }
}

/// Shrinks a little while pressed
class _Pressable extends StatefulWidget {
  const _Pressable({required this.child, required this.onTap});

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
