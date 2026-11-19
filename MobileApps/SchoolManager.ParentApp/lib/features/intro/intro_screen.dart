import 'package:flutter/material.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';
import '../../core/i18n/app_language.dart';
import '../../core/i18n/language_switch.dart';

/// One slide of the intro: what the app is and what the parent can do with it
class _IntroSlide {
  const _IntroSlide({
    required this.icon,
    required this.color,
    required this.title,
    required this.description,
    required this.points,
    this.showLogo = false,
  });

  final IconData icon;
  final Color color;
  final String title;
  final String description;
  final List<String> points;
  final bool showLogo;
}

/// A getter: the texts follow the app language
List<_IntroSlide> get _slides => <_IntroSlide>[
  _IntroSlide(
    icon: Icons.school_rounded,
    color: AppTheme.blue,
    showLogo: true,
    title: tr('مرحباً بك في تطبيق ولي الأمر', 'Welcome to the Parent App'),
    description:
        tr('تطبيق المدرسة الذكية لأولياء الأمور، يربطك بمدرسة أبنائك ويتيح لك متابعة يومهم الدراسي لحظة بلحظة من مكان واحد.', 'The smart school app for parents connects you with your children’s school and lets you follow their school day, moment by moment, in one place.'),
    points: [
      tr('متابعة جميع أبنائك من حساب واحد', 'Follow all your children from one account'),
      tr('معلومات محدثة مباشرة من المدرسة', 'Up-to-date information straight from the school'),
    ],
  ),
  _IntroSlide(
    icon: Icons.insights_rounded,
    color: Color(0xFF31B77A),
    title: tr('متابعة المستوى الدراسي', 'Follow their progress'),
    description: tr('اطّلع على كل ما يخص المستوى الأكاديمي لأبنائك بشكل واضح ومنظم.', 'See everything about your children’s academic progress, clearly organised.'),
    points: [
      tr('نسبة الحضور والغياب', 'Attendance rate'),
      tr('الواجبات المطلوبة ومواعيد تسليمها', 'Homework and due dates'),
      tr('مواعيد الاختبارات ونتائجها والدرجات', 'Exam dates, results and grades'),
      tr('الجدول الدراسي الأسبوعي', 'Weekly timetable'),
    ],
  ),
  _IntroSlide(
    icon: Icons.notifications_active_rounded,
    color: Color(0xFFFFB21A),
    title: tr('تواصل دائم مع المدرسة', 'Always in touch with the school'),
    description: tr('لن يفوتك أي جديد؛ تصلك تنبيهات المدرسة وإعلاناتها فور صدورها.', 'Never miss anything: school alerts and announcements reach you as soon as they are published.'),
    points: [
      tr('الإشعارات والإعلانات المهمة', 'Notifications and important announcements'),
      tr('الفعاليات والرحلات المدرسية', 'Events and school trips'),
      tr('الرسوم الدراسية والمستحقات', 'School fees and payments due'),
      tr('متابعة حالة الباص المدرسي', 'School bus tracking'),
    ],
  ),
];

/// First page of the app: a short definition of the app, then the login page
class IntroScreen extends StatefulWidget {
  const IntroScreen({super.key});

  @override
  State<IntroScreen> createState() => _IntroScreenState();
}

class _IntroScreenState extends State<IntroScreen> {
  final _pageController = PageController();
  int _page = 0;

  bool get _isLast => _page == _slides.length - 1;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  void _goToLogin() => Navigator.of(context).pushReplacementNamed(AppRoutes.login);

  void _next() {
    if (_isLast) {
      _goToLogin();
    } else {
      _pageController.nextPage(duration: const Duration(milliseconds: 350), curve: Curves.easeOutCubic);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: context.palette.card,
      body: SafeArea(
        child: Column(
          children: [
            // Language at the start, Skip at the end (Skip hidden on the last slide)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
              child: Row(
                children: [
                  const LanguageToggleButton(),
                  const Spacer(),
                  AnimatedOpacity(
                  opacity: _isLast ? 0 : 1,
                  duration: const Duration(milliseconds: 200),
                  child: TextButton(
                    onPressed: _isLast ? null : _goToLogin,
                    child: Text(tr('تخطي', 'Skip'), style: TextStyle(fontWeight: FontWeight.bold)),
                  ),
                ),
                ],
              ),
            ),
            Expanded(
              child: PageView.builder(
                controller: _pageController,
                itemCount: _slides.length,
                onPageChanged: (i) => setState(() => _page = i),
                itemBuilder: (context, i) => _SlideView(slide: _slides[i]),
              ),
            ),
            // Page dots
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: List.generate(_slides.length, (i) {
                final active = i == _page;
                return AnimatedContainer(
                  duration: const Duration(milliseconds: 250),
                  margin: const EdgeInsets.symmetric(horizontal: 4),
                  width: active ? 26 : 8,
                  height: 8,
                  decoration: BoxDecoration(
                    color: active ? AppTheme.blue : context.palette.dot,
                    borderRadius: BorderRadius.circular(8),
                  ),
                );
              }),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 24, 24, 24),
              child: SizedBox(
                width: double.infinity,
                height: 54,
                child: FilledButton(
                  onPressed: _next,
                  style: FilledButton.styleFrom(
                    backgroundColor: AppTheme.blue,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                  ),
                  child: Text(
                    _isLast ? tr('ابدأ الآن', 'Get started') : tr('التالي', 'Next'),
                    style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SlideView extends StatelessWidget {
  const _SlideView({required this.slide});

  final _IntroSlide slide;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 28),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 480),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 190,
                height: 190,
                padding: const EdgeInsets.all(22),
                decoration: BoxDecoration(
                  color: slide.color.withValues(alpha: .10),
                  shape: BoxShape.circle,
                ),
                child: slide.showLogo
                    ? Image.asset('assets/images/logo.png', fit: BoxFit.contain)
                    : Icon(slide.icon, size: 88, color: slide.color),
              ),
              const SizedBox(height: 30),
              Text(
                slide.title,
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 23, fontWeight: FontWeight.bold, color: context.palette.ink),
              ),
              const SizedBox(height: 12),
              Text(
                slide.description,
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 14.5, height: 1.7, color: context.palette.muted),
              ),
              const SizedBox(height: 22),
              ...slide.points.map(
                (point) => Padding(
                  padding: const EdgeInsets.only(bottom: 10),
                  child: Row(
                    children: [
                      Icon(Icons.check_circle_rounded, color: slide.color, size: 21),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(point, style: TextStyle(fontSize: 14, color: context.palette.ink)),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
