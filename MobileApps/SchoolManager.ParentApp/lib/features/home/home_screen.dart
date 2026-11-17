import 'package:flutter/material.dart';
import '../../core/auth/auth_service.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';
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
      {super.key, this.loadChildren, this.loadProfile, this.loadTimetable, this.loadFees, this.loadExams, this.loadEvents, this.loadSchoolInfo});

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

  @override
  void initState() {
    super.initState();
    _loadChildren();
    _loadProfile();
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
      _showMessage('تعذر تحميل الملف الشخصي، حاول مرة أخرى.');
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
                    _sectionTitle('الخدمات'),
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
              const CircleAvatar(
                radius: 25,
                backgroundColor: Colors.white24,
                child:
                    Icon(Icons.school_rounded, color: Colors.white, size: 28),
              ),
              const Spacer(),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    'مرحباً،',
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
                  const Text('ولي أمر الطالب',
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
              color: Colors.white,
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
      return const Row(
        children: [
          SizedBox(
              width: 22,
              height: 22,
              child: CircularProgressIndicator(strokeWidth: 2.5)),
          SizedBox(width: 12),
          Expanded(
              child: Text('جاري تحميل بيانات الأبناء...',
                  style: TextStyle(color: AppTheme.ink))),
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
                  style: const TextStyle(fontSize: 12, color: AppTheme.ink))),
          TextButton(
              onPressed: _loadChildren, child: const Text('إعادة المحاولة')),
        ],
      );
    }

    if (children.isEmpty) {
      return const Row(
        children: [
          Icon(Icons.info_outline_rounded,
              color: Color.fromARGB(255, 224, 72, 163)),
          SizedBox(width: 10),
          Expanded(
            child: Text('لا يوجد أبناء مرتبطون بحسابك حالياً.',
                style: TextStyle(color: AppTheme.ink)),
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
      alignment: Alignment.centerRight,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(child.name,
              style: const TextStyle(
                  fontWeight: FontWeight.bold, color: AppTheme.ink)),
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
        'الجدول الدراسي',
        const Color(0xFF1677E8),
        _openTimetable
      ),
      (Icons.menu_book_rounded, 'الواجبات', const Color(0xFF20A66A), null),
      (
        Icons.fact_check_rounded,
        'الاختبارات والدرجات',
        const Color(0xFF8957E5),
        _openExams
      ),
      (
        Icons.how_to_reg_rounded,
        'الحضور والغياب',
        const Color(0xFF0FA5A5),
        null
      ),
      (
        Icons.payments_rounded,
        'الرسوم الدراسية',
        const Color(0xFFF08C00),
        _openFees
      ),
      (Icons.celebration_rounded, 'الفعاليات', const Color(0xFFE0457B), _openEvents),
      (Icons.local_hospital_rounded, 'العيادة الصحية', const Color(0xFFD93025), null),
      (Icons.directions_bus_rounded, 'الباص', const Color(0xFFB7791F), null),
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
      color: Colors.white,
      borderRadius: BorderRadius.circular(18),
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: onTap ?? () => _showMessage('خدمة "$label" ستتوفر قريباً.'),
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
                  color: available ? AppTheme.ink : const Color(0xFF8A96B0),
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
          ? 'جاري تحميل بيانات الأبناء...'
          : 'لا يوجد أبناء مرتبطون بحسابك.');
      return;
    }
    Navigator.of(context).push(MaterialPageRoute(builder: (_) => page(child)));
  }

  Widget _buildAnnouncement() {
    return Container(
      padding: const EdgeInsets.all(17),
      decoration: BoxDecoration(
        color: const Color(0xFFDCEAFF),
        borderRadius: BorderRadius.circular(22),
      ),
      child: const Row(
        children: [
          Icon(Icons.campaign_rounded, color: AppTheme.blue, size: 38),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text('إعلان هام',
                    style: TextStyle(
                        color: AppTheme.navy,
                        fontWeight: FontWeight.bold,
                        fontSize: 17)),
                SizedBox(height: 5),
                Text('موعد الرحلة المدرسية القادمة يوم الخميس القادم.',
                    textAlign: TextAlign.right,
                    style: TextStyle(color: AppTheme.ink, fontSize: 12)),
              ],
            ),
          ),
          Text('عرض الكل',
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
          alignment: Alignment.centerRight,
          child: Text(title,
              style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: AppTheme.ink)),
        ),
      );

  Widget _bottomNavigation() {
    const items = [
      (Icons.home_rounded, 'الرئيسية'),
      (Icons.notifications_rounded, 'الإشعارات'),
      (Icons.chat_bubble_rounded, 'المحادثات'),
      (Icons.menu_rounded, 'المزيد'),
    ];

    return NavigationBar(
      selectedIndex: selectedTab,
      onDestinationSelected: (index) {
        if (index == 3) {
          _showMoreMenu();
          return;
        }
        setState(() => selectedTab = index);
        if (index != 0)
          _showMessage(
              'صفحة ${items[index].$2} سيتم تنفيذها في المرحلة التالية.');
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
    if (p != null)
      return p.email.isNotEmpty && !p.hasPlaceholderEmail
          ? p.email
          : p.loginName;
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
      builder: (sheetContext) => SafeArea(
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
              title: const Text('تعديل الملف الشخصي'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _openEditProfile();
              },
            ),
            ListTile(
              leading: const Icon(Icons.school_rounded, color: AppTheme.blue),
              title: const Text('عن المدرسة'),
              subtitle: const Text('بيانات المدرسة وألبوم الصور'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                Navigator.of(context).push(MaterialPageRoute(
                  builder: (_) => SchoolInfoScreen(loadSchoolInfo: widget.loadSchoolInfo),
                ));
              },
            ),
            ListTile(
              leading:
                  const Icon(Icons.logout_rounded, color: Color(0xFFD93025)),
              title: const Text('تسجيل الخروج',
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
