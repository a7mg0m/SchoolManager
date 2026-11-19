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
    Navigator.of(context).push(MaterialPageRoute(
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

    final saved = await Navigator.of(context).push<bool>(
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            _buildHeader(),
            Expanded(
              child: RefreshIndicator(
                onRefresh: _refresh,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.fromLTRB(18, 18, 18, 18),
                  // Home: the important announcement first, then the services of the selected child
                  children: [
                    _buildAnnouncement(),
                    const SizedBox(height: 22),
                    _sectionTitle(tr('الخدمات', 'Services')),
                    _buildServices(),
                    const SizedBox(height: 20),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: _bottomNavigation(),
    );
  }

  Widget _buildHeader() {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 18, 20, 20),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          colors: [Color(0xFF1677D2), Color(0xFF06468F)],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.vertical(bottom: Radius.circular(34)),
      ),
      child: Column(
        children: [
          Row(
            children: [
              _schoolLogo(),
              const Spacer(),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    tr('مرحباً،', 'Welcome,'),
                    style: TextStyle(
                        color: Colors.white.withValues(alpha: .8),
                        fontSize: 14),
                  ),
                  Text(
                    // Profile name, else the name saved at login (demo name only without a session, e.g. tests)
                    profile?.displayName ??
                        AuthService.instance.session?.displayName ??
                        parentName,
                    style: const TextStyle(
                        color: Colors.white,
                        fontSize: 21,
                        fontWeight: FontWeight.bold),
                  ),
                  Text(tr('ولي أمر الطالب', 'Parent'),
                      style: TextStyle(color: Colors.white70, fontSize: 12)),
                ],
              ),
              const SizedBox(width: 12),
              Stack(
                clipBehavior: Clip.none,
                children: [
                  _profileAvatar(),
                  Positioned(
                    top: -4,
                    left: -5,
                    child: Container(
                      padding: const EdgeInsets.all(5),
                      decoration: const BoxDecoration(
                          color: Colors.red, shape: BoxShape.circle),
                      child: const Text('2',
                          style: TextStyle(color: Colors.white, fontSize: 10)),
                    ),
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 22),
          Container(
            constraints: const BoxConstraints(minHeight: 62),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
            decoration: BoxDecoration(
              color: context.palette.card,
              borderRadius: BorderRadius.circular(22),
            ),
            child: _buildChildSelector(),
          ),
        ],
      ),
    );
  }

  /// Profile picture from My Profile beside the name; person icon when there is none
  Widget _profileAvatar() {
    final image = profile?.image;
    // Tap the picture to edit the profile
    return GestureDetector(
      onTap: _openEditProfile,
      child: Container(
        padding: const EdgeInsets.all(2),
        decoration:
            const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
        child: CircleAvatar(
          radius: 24,
          backgroundColor: const Color(0xFFFFC044),
          foregroundImage: image != null ? MemoryImage(image) : null,
          // Shown while there is no image, or if the image cannot be decoded
          onForegroundImageError: image != null ? (_, __) {} : null,
          child: const Icon(Icons.person, color: Colors.white, size: 30),
        ),
      ),
    );
  }

  /// Children of the signed-in parent: loading / error / none / one / a dropdown for several
  Widget _buildChildSelector() {
    if (isLoadingChildren) {
      return Row(
        children: [
          SizedBox(
              width: 22,
              height: 22,
              child: CircularProgressIndicator(strokeWidth: 2.5)),
          SizedBox(width: 12),
          Expanded(
              child: Text(tr('جاري تحميل بيانات الأبناء...', 'Loading your children...'),
                  style: TextStyle(color: context.palette.ink))),
        ],
      );
    }

    if (childrenError != null) {
      return Row(
        children: [
          const Icon(Icons.error_outline_rounded, color: Color(0xFFD93025)),
          const SizedBox(width: 10),
          Expanded(
              child: Text(childrenError!,
                  style: TextStyle(fontSize: 12, color: context.palette.ink))),
          TextButton(
              onPressed: _loadChildren, child: Text(tr('إعادة المحاولة', 'Try again'))),
        ],
      );
    }

    if (children.isEmpty) {
      return Row(
        children: [
          Icon(Icons.info_outline_rounded,
              color: Color.fromARGB(255, 224, 72, 163)),
          SizedBox(width: 10),
          Expanded(
            child: Text(tr('لا يوجد أبناء مرتبطون بحسابك حالياً.', 'No children are linked to your account yet.'),
                style: TextStyle(color: context.palette.ink)),
          ),
        ],
      );
    }

    final child = selectedChild ?? children.first;
    return Row(
      children: [
        Text(child.avatar, style: const TextStyle(fontSize: 34)),
        const SizedBox(width: 10),
        Expanded(
          child: children.length == 1
              // One child: no dropdown needed
              ? _childLabel(child)
              : DropdownButtonHideUnderline(
                  child: DropdownButton<Child>(
                    value: child,
                    isExpanded: true,
                    // Two lines per child (name + grade): let rows grow instead of overflowing 48px
                    itemHeight: null,
                    icon: const Icon(Icons.keyboard_arrow_down_rounded,
                        color: AppTheme.blue),
                    borderRadius: BorderRadius.circular(16),
                    items: children
                        .map((c) =>
                            DropdownMenuItem(value: c, child: _childLabel(c)))
                        .toList(),
                    onChanged: (c) {
                      if (c != null) setState(() => selectedChild = c);
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Widget _childLabel(Child child) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(child.name,
              style: TextStyle(
                  fontWeight: FontWeight.bold, color: context.palette.ink)),
          if (child.classInfo.isNotEmpty)
            Text(child.classInfo,
                style: const TextStyle(fontSize: 11, color: Colors.black54)),
        ],
      ),
    );
  }

  /// Services of the selected child as icons; only the timetable is available so far
  Widget _buildServices() {
    final services = <(IconData, String, Color, VoidCallback?)>[
      (
        Icons.calendar_view_week_rounded,
        tr('الجدول الدراسي', 'Timetable'),
        const Color(0xFF1677E8),
        _openTimetable
      ),
      (Icons.menu_book_rounded, tr('الواجبات', 'Homework'), const Color(0xFF20A66A), null),
      (
        Icons.fact_check_rounded,
        tr('الاختبارات والدرجات', 'Exams & Grades'),
        const Color(0xFF8957E5),
        _openExams
      ),
      (
        Icons.how_to_reg_rounded,
        tr('الحضور والغياب', 'Attendance'),
        const Color(0xFF0FA5A5),
        null
      ),
      (
        Icons.payments_rounded,
        tr('الرسوم الدراسية', 'School Fees'),
        const Color(0xFFF08C00),
        _openFees
      ),
      (
        Icons.celebration_rounded,
        tr('الفعاليات', 'Events'),
        const Color(0xFFE0457B),
        _openEvents
      ),
      (
        Icons.local_hospital_rounded,
        tr('العيادة الصحية', 'School Clinic'),
        const Color(0xFFD93025),
        null
      ),
      (Icons.directions_bus_rounded, tr('الباص', 'School Bus'), const Color(0xFFB7791F), null),
    ];

    return LayoutBuilder(
      builder: (context, constraints) {
        // 4 per row on phones, more on wide screens
        final perRow = constraints.maxWidth >= 600 ? 6 : 4;
        const gap = 10.0;
        final width = (constraints.maxWidth - gap * (perRow - 1)) / perRow;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final (icon, label, color, onTap) in services)
              SizedBox(
                  width: width, child: _serviceTile(icon, label, color, onTap)),
          ],
        );
      },
    );
  }

  Widget _serviceTile(
      IconData icon, String label, Color color, VoidCallback? onTap) {
    final available = onTap != null;
    return Material(
      color: context.palette.card,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: onTap ?? () => _showMessage(tr('خدمة "$label" ستتوفر قريباً.', 'The "$label" service is coming soon.')),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 4),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(11),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: available ? .13 : .07),
                  borderRadius: BorderRadius.circular(14),
                ),
                child: Icon(icon,
                    size: 26,
                    color: available ? color : color.withValues(alpha: .45)),
              ),
              const SizedBox(height: 7),
              Text(
                label,
                textAlign: TextAlign.center,
                maxLines: 2,
                style: TextStyle(
                  fontSize: 11.5,
                  height: 1.25,
                  fontWeight: FontWeight.w600,
                  color: available ? context.palette.ink : context.palette.faint,
                ),
              ),
            ],
          ),
        ),
      ),
    );
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
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => page(child)));
  }

  Widget _buildAnnouncement() {
    return Container(
      padding: const EdgeInsets.all(17),
      decoration: BoxDecoration(
        color: context.palette.infoTint,
        borderRadius: BorderRadius.circular(22),
      ),
      child: Row(
        children: [
          Icon(Icons.campaign_rounded, color: AppTheme.blue, size: 38),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(tr('إعلان هام', 'Important notice'),
                    style: TextStyle(
                        color: AppTheme.navy,
                        fontWeight: FontWeight.bold,
                        fontSize: 17)),
                SizedBox(height: 5),
                Text(tr('موعد الرحلة المدرسية القادمة يوم الخميس القادم.', 'The next school trip is next Thursday.'),
                    textAlign: TextAlign.right,
                    style: TextStyle(color: context.palette.ink, fontSize: 12)),
              ],
            ),
          ),
          Text(tr('عرض الكل', 'View all'),
              style: TextStyle(
                  color: AppTheme.blue,
                  fontWeight: FontWeight.bold,
                  fontSize: 11)),
        ],
      ),
    );
  }

  Widget _sectionTitle(String title) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: Align(
          alignment: AlignmentDirectional.centerStart,
          child: Text(title,
              style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: context.palette.ink)),
        ),
      );

  Widget _bottomNavigation() {
    final items = [
      (Icons.home_rounded, tr('الرئيسية', 'Home')),
      (Icons.notifications_rounded, tr('الإشعارات', 'Notifications')),
      (Icons.chat_bubble_rounded, tr('المحادثات', 'Chats')),
      (Icons.menu_rounded, tr('المزيد', 'More')),
    ];

    return NavigationBar(
      selectedIndex: selectedTab,
      onDestinationSelected: (index) {
        if (index == 3) {
          _showMoreMenu();
          return;
        }
        setState(() => selectedTab = index);
        if (index != 0) {
          _showMessage(
              tr('صفحة ${items[index].$2} سيتم تنفيذها في المرحلة التالية.', 'The ${items[index].$2} page is coming in the next phase.'));
        }
      },
      destinations: items
          .map((item) => NavigationDestination(
                icon: Icon(item.$1),
                label: item.$2,
              ))
          .toList(),
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
                Navigator.of(context).push(MaterialPageRoute(
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
