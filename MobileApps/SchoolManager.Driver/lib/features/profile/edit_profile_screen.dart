import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/theme/app_theme.dart';
import 'profile_service.dart';
import '../../core/i18n/app_language.dart';

typedef SaveProfile = Future<MyProfile> Function({
  required String firstName,
  required String lastName,
  required String email,
  required String phoneNumber,
  required String address,
  Uint8List? image,
  bool removeImage,
});

typedef ChangePassword = Future<void> Function(String currentPassword, String newPassword);

/// Edit the signed-in parent's profile (name, contact details, picture) and change the password.
/// Saving the profile returns to home right away; pops with true when something was saved,
/// so the home screen reloads its data.
class EditProfileScreen extends StatefulWidget {
  const EditProfileScreen({
    super.key,
    required this.profile,
    this.saveProfile,
    this.changePassword,
    this.pickImage,
  });

  final MyProfile profile;

  /// Defaults to the API (tests pass their own)
  final SaveProfile? saveProfile;
  final ChangePassword? changePassword;
  final Future<Uint8List?> Function(ImageSource source)? pickImage;

  @override
  State<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends State<EditProfileScreen> {
  final _profileForm = GlobalKey<FormState>();
  final _passwordForm = GlobalKey<FormState>();

  late final _firstName = TextEditingController(text: widget.profile.firstName);
  late final _lastName = TextEditingController(text: widget.profile.lastName);
  // A placeholder email (parent without email) is not shown; an empty field keeps it
  late final _email = TextEditingController(text: widget.profile.hasPlaceholderEmail ? '' : widget.profile.email);
  late final _phone = TextEditingController(text: widget.profile.phoneNumber);
  late final _address = TextEditingController(text: widget.profile.address);

  final _currentPassword = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirmPassword = TextEditingController();

  late Uint8List? _image = widget.profile.image;
  Uint8List? _newImage;
  bool _removeImage = false;

  bool _isSavingProfile = false;
  bool _isChangingPassword = false;
  bool _hidePasswords = true;
  bool _saved = false;

  @override
  void dispose() {
    for (final c in [_firstName, _lastName, _email, _phone, _address, _currentPassword, _newPassword, _confirmPassword]) {
      c.dispose();
    }
    super.dispose();
  }

  // ----- Picture -----

  Future<Uint8List?> _pick(ImageSource source) async {
    if (widget.pickImage != null) return widget.pickImage!(source);
    // Small square-ish picture: well under the 1 MB the API accepts
    final file = await ImagePicker().pickImage(source: source, maxWidth: 512, maxHeight: 512, imageQuality: 80);
    return file?.readAsBytes();
  }

  Future<void> _choosePicture() async {
    final choice = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined, color: AppTheme.blue),
              title: Text(tr('اختيار من المعرض', 'Choose from gallery')),
              onTap: () => Navigator.pop(sheet, 'gallery'),
            ),
            if (!kIsWeb)
              ListTile(
                leading: const Icon(Icons.photo_camera_outlined, color: AppTheme.blue),
                title: Text(tr('التقاط صورة', 'Take a photo')),
                onTap: () => Navigator.pop(sheet, 'camera'),
              ),
            if (_image != null)
              ListTile(
                leading: const Icon(Icons.delete_outline, color: Color(0xFFD93025)),
                title: Text(tr('حذف الصورة', 'Remove photo'), style: TextStyle(color: Color(0xFFD93025))),
                onTap: () => Navigator.pop(sheet, 'remove'),
              ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );
    if (!mounted || choice == null) return;

    if (choice == 'remove') {
      setState(() {
        _image = null;
        _newImage = null;
        _removeImage = true;
      });
      return;
    }

    try {
      final bytes = await _pick(choice == 'camera' ? ImageSource.camera : ImageSource.gallery);
      if (!mounted || bytes == null) return;
      if (bytes.length > ProfileService.maxImageBytes) {
        _showMessage(tr('الصورة كبيرة جداً، اختر صورة أصغر.', 'The photo is too large. Choose a smaller one.'));
        return;
      }
      setState(() {
        _image = bytes;
        _newImage = bytes;
        _removeImage = false;
      });
    } catch (_) {
      if (mounted) _showMessage(tr('تعذر فتح الصورة.', 'Could not open the photo.'));
    }
  }

  // ----- Save -----

  Future<void> _saveProfile() async {
    FocusScope.of(context).unfocus();
    if (!(_profileForm.currentState?.validate() ?? false)) return;

    final email = _email.text.trim().isEmpty && widget.profile.hasPlaceholderEmail
        ? widget.profile.email // keep the placeholder
        : _email.text.trim();

    setState(() => _isSavingProfile = true);
    try {
      final save = widget.saveProfile ?? ProfileService.instance.updateMyProfile;
      await save(
        firstName: _firstName.text,
        lastName: _lastName.text,
        email: email,
        phoneNumber: _phone.text,
        address: _address.text,
        image: _newImage,
        removeImage: _removeImage,
      );
      _saved = true;
      _newImage = null;
      _removeImage = false;
      if (!mounted) return;
      // The app-wide messenger keeps the message visible on the home screen after the pop
      _showMessage(tr('تم حفظ الملف الشخصي.', 'Profile saved.'));
      // Back to home, which refreshes its data
      final navigator = Navigator.of(context);
      if (navigator.canPop()) navigator.pop(true);
    } on ProfileException catch (e) {
      if (mounted) _showMessage(e.message);
    } on SessionExpiredException {
      if (mounted) Navigator.of(context).pop(_saved);
    } finally {
      if (mounted) setState(() => _isSavingProfile = false);
    }
  }

  Future<void> _changePassword() async {
    FocusScope.of(context).unfocus();
    if (!(_passwordForm.currentState?.validate() ?? false)) return;

    setState(() => _isChangingPassword = true);
    try {
      final change = widget.changePassword ?? ProfileService.instance.changePassword;
      await change(_currentPassword.text, _newPassword.text);
      _currentPassword.clear();
      _newPassword.clear();
      _confirmPassword.clear();
      if (mounted) _showMessage(tr('تم تغيير كلمة المرور.', 'Password changed.'));
    } on ProfileException catch (e) {
      if (mounted) _showMessage(e.message);
    } on SessionExpiredException {
      if (mounted) Navigator.of(context).pop(_saved);
    } finally {
      if (mounted) setState(() => _isChangingPassword = false);
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  // ----- UI -----

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) Navigator.of(context).pop(_saved);
      },
      child: Scaffold(
        appBar: AppBar(title: Text(tr('الملف الشخصي', 'My Profile'))),
        body: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 560),
              child: ListView(
                padding: const EdgeInsets.all(18),
                children: [
                  _avatar(),
                  const SizedBox(height: 6),
                  Center(
                    child: Text(widget.profile.loginName,
                        textDirection: TextDirection.ltr,
                        style: TextStyle(color: context.palette.muted)),
                  ),
                  const SizedBox(height: 18),
                  _profileCard(),
                  const SizedBox(height: 18),
                  _passwordCard(),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _avatar() {
    return Center(
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          CircleAvatar(
            radius: 54,
            backgroundColor: const Color(0xFFFFC044),
            foregroundImage: _image != null ? MemoryImage(_image!) : null,
            onForegroundImageError: _image != null ? (_, _) {} : null,
            child: const Icon(Icons.person, color: Colors.white, size: 60),
          ),
          Positioned(
            bottom: -4,
            left: -4,
            child: Material(
              color: AppTheme.blue,
              shape: const CircleBorder(side: BorderSide(color: Colors.white, width: 3)),
              child: IconButton(
                tooltip: tr('تغيير الصورة', 'Change photo'),
                icon: const Icon(Icons.photo_camera_rounded, color: Colors.white, size: 20),
                onPressed: _isSavingProfile ? null : _choosePicture,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _profileCard() {
    return _card(
      title: tr('البيانات الشخصية', 'Personal details'),
      child: Form(
        key: _profileForm,
        child: Column(
          children: [
            Row(
              children: [
                Expanded(child: _field(_firstName, tr('الاسم الأول', 'First name'), Icons.person_outline, required: true, maxLength: 100)),
                const SizedBox(width: 10),
                Expanded(child: _field(_lastName, tr('اسم العائلة', 'Last name'), Icons.badge_outlined, required: true, maxLength: 100)),
              ],
            ),
            _field(
              _email,
              tr('البريد الإلكتروني', 'Email'),
              Icons.email_outlined,
              keyboard: TextInputType.emailAddress,
              ltr: true,
              required: !widget.profile.hasPlaceholderEmail,
              helper: widget.profile.hasPlaceholderEmail ? tr('اختياري', 'Optional') : null,
              validator: (v) {
                final value = v?.trim() ?? '';
                if (value.isNotEmpty && !RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(value)) {
                  return tr('البريد الإلكتروني غير صحيح', 'Invalid email address');
                }
                return null;
              },
            ),
            _field(_phone, tr('رقم الجوال', 'Mobile number'), Icons.phone_outlined, keyboard: TextInputType.phone, ltr: true, maxLength: 50),
            _field(_address, tr('العنوان', 'Address'), Icons.home_outlined, maxLength: 500, lines: 2),
            // Set by the school; shown here but not editable
            if (widget.profile.jobLevel.isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: TextFormField(
                  key: const ValueKey('jobLevelField'),
                  initialValue: widget.profile.jobLevel,
                  readOnly: true,
                  enabled: false,
                  decoration: InputDecoration(
                    labelText: tr('المستوى الوظيفي', 'Job level'),
                    helperText: tr('تحدده إدارة المدرسة', 'Set by the school'),
                    prefixIcon: const Icon(Icons.workspace_premium_outlined),
                    suffixIcon: const Icon(Icons.lock_outline, size: 18),
                    filled: true,
                    fillColor: context.palette.field,
                    border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
                  ),
                ),
              ),
            const SizedBox(height: 6),
            _button(tr('حفظ التعديلات', 'Save changes'), _isSavingProfile, _saveProfile),
          ],
        ),
      ),
    );
  }

  Widget _passwordCard() {
    return _card(
      title: tr('تغيير كلمة المرور', 'Change password'),
      child: Form(
        key: _passwordForm,
        child: Column(
          children: [
            _field(_currentPassword, tr('كلمة المرور الحالية', 'Current password'), Icons.lock_outline,
                password: true, ltr: true, required: true),
            _field(_newPassword, tr('كلمة المرور الجديدة', 'New password'), Icons.lock_reset_outlined,
                password: true, ltr: true, required: true, validator: (v) {
              if ((v ?? '').length < 6) return tr('كلمة المرور يجب ألا تقل عن 6 أحرف', 'Password must be at least 6 characters');
              if (v == _currentPassword.text) return tr('كلمة المرور الجديدة يجب أن تختلف عن الحالية', 'The new password must be different from the current one');
              return null;
            }),
            _field(_confirmPassword, tr('تأكيد كلمة المرور', 'Confirm password'), Icons.lock_reset_outlined,
                password: true, ltr: true, required: true,
                validator: (v) => v != _newPassword.text ? tr('كلمتا المرور غير متطابقتين', 'The passwords do not match') : null),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: TextButton.icon(
                onPressed: () => setState(() => _hidePasswords = !_hidePasswords),
                icon: Icon(_hidePasswords ? Icons.visibility_outlined : Icons.visibility_off_outlined, size: 18),
                label: Text(_hidePasswords ? tr('إظهار كلمات المرور', 'Show passwords') : tr('إخفاء كلمات المرور', 'Hide passwords')),
              ),
            ),
            _button(tr('تغيير كلمة المرور', 'Change password'), _isChangingPassword, _changePassword),
          ],
        ),
      ),
    );
  }

  Widget _card({required String title, required Widget child}) {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
      decoration: BoxDecoration(color: context.palette.card, borderRadius: BorderRadius.circular(20)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(title, style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: context.palette.ink)),
          const SizedBox(height: 14),
          child,
        ],
      ),
    );
  }

  Widget _field(
    TextEditingController controller,
    String label,
    IconData icon, {
    bool required = false,
    bool password = false,
    bool ltr = false,
    int? maxLength,
    int lines = 1,
    String? helper,
    TextInputType? keyboard,
    String? Function(String?)? validator,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextFormField(
        controller: controller,
        obscureText: password && _hidePasswords,
        keyboardType: keyboard,
        textDirection: ltr ? TextDirection.ltr : null,
        maxLines: password ? 1 : lines,
        decoration: InputDecoration(
          labelText: label,
          helperText: helper,
          prefixIcon: Icon(icon),
          filled: true,
          fillColor: context.palette.field,
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
        ),
        validator: (v) {
          if (required && (v == null || v.trim().isEmpty)) return tr('هذا الحقل مطلوب', 'This field is required');
          if (maxLength != null && (v?.trim().length ?? 0) > maxLength) return tr('الحد الأقصى $maxLength حرف', 'Maximum $maxLength characters');
          return validator?.call(v);
        },
      ),
    );
  }

  Widget _button(String text, bool busy, VoidCallback onPressed) {
    return SizedBox(
      height: 50,
      child: FilledButton(
        onPressed: busy ? null : onPressed,
        style: FilledButton.styleFrom(
          backgroundColor: AppTheme.blue,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        ),
        child: busy
            ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.5, color: Colors.white))
            : Text(text, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
      ),
    );
  }
}
