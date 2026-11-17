import 'package:flutter/material.dart';
import '../../core/routes.dart';
import '../../core/theme/app_theme.dart';

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

const _slides = <_IntroSlide>[
  _IntroSlide(
    icon: Icons.school_rounded,
    color: AppTheme.blue,
    showLogo: true,
    title: 'مرحباً بك في تطبيق ولي الأمر',
    description:
        'تطبيق المدرسة الذكية لأولياء الأمور، يربطك بمدرسة أبنائك ويتيح لك متابعة يومهم الدراسي لحظة بلحظة من مكان واحد.',
    points: [
      'متابعة جميع أبنائك من حساب واحد',
      'معلومات محدثة مباشرة من المدرسة',
    ],
  ),
  _IntroSlide(
    icon: Icons.insights_rounded,
    color: Color(0xFF31B77A),
    title: 'متابعة المستوى الدراسي',
    description: 'اطّلع على كل ما يخص المستوى الأكاديمي لأبنائك بشكل واضح ومنظم.',
    points: [
      'نسبة الحضور والغياب',
      'الواجبات المطلوبة ومواعيد تسليمها',
      'مواعيد الاختبارات ونتائجها والدرجات',
      'الجدول الدراسي الأسبوعي',
    ],
  ),
  _IntroSlide(
    icon: Icons.notifications_active_rounded,
    color: Color(0xFFFFB21A),
    title: 'تواصل دائم مع المدرسة',
    description: 'لن يفوتك أي جديد؛ تصلك تنبيهات المدرسة وإعلاناتها فور صدورها.',
    points: [
      'الإشعارات والإعلانات المهمة',
      'الفعاليات والرحلات المدرسية',
      'الرسوم الدراسية والمستحقات',
      'متابعة حالة الباص المدرسي',
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
      backgroundColor: Colors.white,
      body: SafeArea(
        child: Column(
          children: [
            // Skip (hidden on the last slide)
            Padding(
              padding: const EdgeInsets.fromLTRB(12, 8, 12, 0),
              child: Align(
                alignment: AlignmentDirectional.centerEnd,
                child: AnimatedOpacity(
                  opacity: _isLast ? 0 : 1,
                  duration: const Duration(milliseconds: 200),
                  child: TextButton(
                    onPressed: _isLast ? null : _goToLogin,
                    child: const Text('تخطي', style: TextStyle(fontWeight: FontWeight.bold)),
                  ),
                ),
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
                    color: active ? AppTheme.blue : const Color(0xFFD3DDEE),
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
                    _isLast ? 'ابدأ الآن' : 'التالي',
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
                style: const TextStyle(fontSize: 23, fontWeight: FontWeight.bold, color: AppTheme.ink),
              ),
              const SizedBox(height: 12),
              Text(
                slide.description,
                textAlign: TextAlign.center,
                style: const TextStyle(fontSize: 14.5, height: 1.7, color: Color(0xFF53668D)),
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
                        child: Text(point, style: const TextStyle(fontSize: 14, color: AppTheme.ink)),
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
