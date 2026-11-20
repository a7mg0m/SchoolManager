import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/i18n/app_language.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import '../schedule/schedule_format.dart';
import 'clinic_service.dart';

const _red = Color(0xFFD93025);
const _green = Color(0xFF13864B);
const _orange = Color(0xFFF08C00);
const _grey = Color(0xFF637394);

typedef LoadMedicalProfile = Future<MedicalProfile?> Function(String studentId);
typedef SaveMedicalProfile = Future<void> Function(String studentId, MedicalProfile profile);
typedef LoadVaccinations = Future<List<ChildVaccination>> Function(String studentId);
typedef LoadClinicVisits = Future<List<ClinicVisit>> Function(String studentId);

/// School clinic of the selected child: health profile (the parent can edit it), vaccinations and clinic visits
class ClinicScreen extends StatefulWidget {
  const ClinicScreen({
    super.key,
    required this.child,
    this.loadProfile,
    this.saveProfile,
    this.loadVaccinations,
    this.loadVisits,
    this.onSessionExpired,
  });

  final Child child;

  /// Default to the API (tests pass their own)
  final LoadMedicalProfile? loadProfile;
  final SaveMedicalProfile? saveProfile;
  final LoadVaccinations? loadVaccinations;
  final LoadClinicVisits? loadVisits;
  final VoidCallback? onSessionExpired;

  @override
  State<ClinicScreen> createState() => _ClinicScreenState();
}

/// One list loaded from the API: value, loading, error
class _Load<T> {
  T? value;
  bool loaded = false;
  bool loading = true;
  String? error;
}

class _ClinicScreenState extends State<ClinicScreen> with SingleTickerProviderStateMixin {
  late final _tabs = TabController(length: 3, vsync: this)..addListener(() => setState(() {}));

  final _profile = _Load<MedicalProfile?>();
  final _vaccinations = _Load<List<ChildVaccination>>();
  final _visits = _Load<List<ClinicVisit>>();

  @override
  void initState() {
    super.initState();
    _loadProfile();
    _loadVaccinations();
    _loadVisits();
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  Future<void> _run<T>(_Load<T> state, Future<T> Function() load) async {
    setState(() {
      state.loading = !state.loaded;
      state.error = null;
    });
    try {
      final result = await load();
      if (!mounted) return;
      setState(() {
        state.value = result;
        state.loaded = true;
        state.loading = false;
      });
    } on ClinicException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        widget.onSessionExpired!();
        return;
      }
      setState(() {
        state.error = e.message;
        state.loading = false;
      });
    }
  }

  Future<void> _loadProfile() =>
      _run(_profile, () => (widget.loadProfile ?? ClinicService.instance.getMedicalProfile)(widget.child.id));

  Future<void> _loadVaccinations() =>
      _run(_vaccinations, () => (widget.loadVaccinations ?? ClinicService.instance.getVaccinations)(widget.child.id));

  Future<void> _loadVisits() =>
      _run(_visits, () => (widget.loadVisits ?? ClinicService.instance.getClinicVisits)(widget.child.id));

  Future<void> _edit() async {
    final saved = await Navigator.of(context).push<bool>(MaterialPageRoute(
      builder: (_) => EditMedicalProfileScreen(
        child: widget.child,
        profile: _profile.value ?? const MedicalProfile(),
        saveProfile: widget.saveProfile,
        onSessionExpired: widget.onSessionExpired,
      ),
    ));
    if (saved == true && mounted) await _loadProfile();
  }

  bool get _hasProfile => _profile.value != null && !_profile.value!.isEmpty;

  @override
  Widget build(BuildContext context) {
    final vaccinations = _vaccinations.value;
    final visits = _visits.value;
    String count(List? list) => list == null || list.isEmpty ? '' : ' (${list.length})';

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('العيادة الصحية', 'School Clinic'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
          ],
        ),
        actions: [
          if (_tabs.index == 0 && _profile.loaded && _hasProfile)
            IconButton(
              tooltip: tr('تعديل الملف الصحي', 'Edit health profile'),
              icon: const Icon(Icons.edit_rounded),
              onPressed: _edit,
            ),
        ],
        bottom: TabBar(
          controller: _tabs,
          isScrollable: true,
          tabAlignment: TabAlignment.start,
          labelColor: Colors.white,
          unselectedLabelColor: Colors.white70,
          indicatorColor: Colors.white,
          tabs: [
            Tab(icon: const Icon(Icons.health_and_safety_rounded, size: 20), text: tr('الملف الصحي', 'Health profile')),
            Tab(icon: const Icon(Icons.vaccines_rounded, size: 20), text: tr('التطعيمات', 'Vaccinations') + count(vaccinations)),
            Tab(icon: const Icon(Icons.medical_services_rounded, size: 20), text: tr('الزيارات', 'Visits') + count(visits)),
          ],
        ),
      ),
      body: SafeArea(
        child: TabBarView(
          controller: _tabs,
          children: [
            _tab(_profile, _loadProfile, (_) => _hasProfile ? _details(_profile.value!) : [_noProfile()]),
            _tab(_vaccinations, _loadVaccinations, (list) => list == null || list.isEmpty
                ? [_empty(Icons.vaccines_outlined, tr('لا توجد تطعيمات مسجلة للطالب.', 'No vaccinations recorded for this student.'))]
                : [for (final v in list) _vaccinationCard(v)]),
            _tab(_visits, _loadVisits, (list) => list == null || list.isEmpty
                ? [_empty(Icons.medical_services_outlined, tr('لم يزر الطالب العيادة بعد.', 'No clinic visits yet.'))]
                : [for (final v in list) _visitCard(v)]),
          ],
        ),
      ),
    );
  }

  /// Loading / error / pull-to-refresh list of one tab
  Widget _tab<T>(_Load<T> state, Future<void> Function() reload, List<Widget> Function(T? value) children) {
    if (state.loading) return const Center(child: CircularProgressIndicator());
    if (state.error != null) {
      return _message(Icons.error_outline_rounded, state.error!, action: (tr('إعادة المحاولة', 'Try again'), reload));
    }
    return RefreshIndicator(
      onRefresh: reload,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 24),
        children: children(state.value),
      ),
    );
  }

  Widget _empty(IconData icon, String text) =>
      Padding(padding: const EdgeInsets.only(top: 60), child: _message(icon, text));

  Widget _noProfile() => Padding(
        padding: const EdgeInsets.only(top: 60),
        child: _message(
          Icons.health_and_safety_outlined,
          tr('لا يوجد ملف صحي لـ ${widget.child.name} بعد.\nأضف فصيلة الدم والحساسية والأدوية ليطّلع عليها طاقم العيادة.',
              'There is no health profile for ${widget.child.name} yet.\nAdd the blood type, allergies and medications so the clinic staff know.'),
          action: (tr('إضافة الملف الصحي', 'Add health profile'), _edit),
        ),
      );

  // ----- Health profile -----

  List<Widget> _details(MedicalProfile p) {
    final notSet = tr('غير محدد', 'Not set');
    final none = tr('لا يوجد', 'None');
    return [
      // Blood type and emergency phone side by side
      Row(
        children: [
          Expanded(child: _keyTile(Icons.bloodtype_rounded, tr('فصيلة الدم', 'Blood type'), p.bloodType.isEmpty ? notSet : p.bloodType, _red)),
          const SizedBox(width: 12),
          Expanded(
            child: _keyTile(Icons.phone_in_talk_rounded, tr('هاتف الطوارئ', 'Emergency phone'),
                p.emergencyPhone.isEmpty ? notSet : p.emergencyPhone, AppTheme.blue,
                ltr: p.emergencyPhone.isNotEmpty),
          ),
        ],
      ),
      const SizedBox(height: 12),
      _section(Icons.warning_amber_rounded, tr('الحساسية', 'Allergies'), p.allergies.isEmpty ? none : p.allergies, _orange,
          highlight: p.allergies.isNotEmpty),
      _section(Icons.monitor_heart_rounded, tr('الأمراض المزمنة', 'Chronic diseases'), p.chronicDiseases.isEmpty ? none : p.chronicDiseases,
          const Color(0xFF9B6BFF)),
      _section(Icons.medication_rounded, tr('الأدوية المنتظمة', 'Regular medications'),
          p.regularMedications.isEmpty ? none : p.regularMedications, const Color(0xFF20A66A)),
      _section(Icons.sticky_note_2_rounded, tr('ملاحظات', 'Notes'), p.notes.isEmpty ? none : p.notes, _grey),
      const SizedBox(height: 8),
      FilledButton.icon(
        onPressed: _edit,
        icon: const Icon(Icons.edit_rounded),
        label: Text(tr('تعديل الملف الصحي', 'Edit health profile')),
      ),
    ];
  }

  Widget _keyTile(IconData icon, String label, String value, Color color, {bool ltr = false}) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withValues(alpha: .08),
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: color.withValues(alpha: .25)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color),
          const SizedBox(height: 8),
          Text(label, style: TextStyle(fontSize: 12, color: context.palette.muted)),
          const SizedBox(height: 2),
          Text(value,
              textDirection: ltr ? TextDirection.ltr : null,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: context.palette.ink)),
        ],
      ),
    );
  }

  Widget _section(IconData icon, String label, String value, Color color, {bool highlight = false}) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: highlight ? color.withValues(alpha: .08) : context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: highlight ? color.withValues(alpha: .35) : context.palette.line),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _iconBubble(icon, color),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(label, style: TextStyle(fontSize: 12, color: context.palette.muted)),
                const SizedBox(height: 3),
                Text(value, style: TextStyle(fontSize: 15, height: 1.4, color: context.palette.ink)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  // ----- Vaccinations -----

  Widget _vaccinationCard(ChildVaccination v) {
    final (label, color) = switch (v.status) {
      VaccinationState.given => (tr('تم التطعيم', 'Given'), _green),
      VaccinationState.notGiven => (tr('لم يُعطَ', 'Not given'), _orange),
      VaccinationState.exempted => (tr('معفى', 'Exempted'), _grey),
      VaccinationState.parentRefused => (tr('رفض ولي الأمر', 'Refused by parent'), _red),
      VaccinationState.unknown => (tr('غير معروف', 'Unknown'), _grey),
    };
    final info = [
      tr('الجرعة ${v.doseNumber}', 'Dose ${v.doseNumber}'),
      if (v.date != null) _date(v.date!),
      if (v.status == VaccinationState.given) v.atSchool ? tr('في المدرسة', 'At school') : tr('خارج المدرسة', 'Outside school'),
    ].join(' · ');

    return _card(
      icon: Icons.vaccines_rounded,
      color: color,
      title: v.vaccineName.isEmpty ? tr('تطعيم', 'Vaccine') : v.vaccineName,
      subtitle: info,
      badge: (label, color),
      children: [
        if (v.notes.isNotEmpty) _line(Icons.sticky_note_2_outlined, v.notes),
      ],
    );
  }

  // ----- Clinic visits -----

  Widget _visitCard(ClinicVisit v) {
    final (label, color) = switch (v.outcome) {
      VisitResult.returnedToClass => (tr('عاد إلى الفصل', 'Back to class'), _green),
      VisitResult.sentHome => (tr('أُرسل إلى المنزل', 'Sent home'), _orange),
      VisitResult.referred => (tr('أُحيل لجهة طبية', 'Referred'), _red),
      VisitResult.unknown => ('', _grey),
    };
    String num(double value) => value == value.roundToDouble() ? value.toInt().toString() : value.toStringAsFixed(1);
    final vitals = [
      if (v.temperature != null) tr('الحرارة ${num(v.temperature!)}°', 'Temp ${num(v.temperature!)}°'),
      if (v.weight != null) tr('الوزن ${num(v.weight!)} كجم', 'Weight ${num(v.weight!)} kg'),
      if (v.height != null) tr('الطول ${num(v.height!)} سم', 'Height ${num(v.height!)} cm'),
    ].join('  ·  ');

    return _card(
      icon: Icons.medical_services_rounded,
      color: color,
      title: v.complaint.isEmpty ? tr('زيارة العيادة', 'Clinic visit') : v.complaint,
      subtitle: [
        '${_date(v.date)} · ${formatTime(v.date.hour * 60 + v.date.minute)}',
        if (v.doctorName.isNotEmpty) v.doctorName,
      ].join(' · '),
      badge: label.isEmpty ? null : (label, color),
      children: [
        if (vitals.isNotEmpty) _line(Icons.thermostat_rounded, vitals),
        if (v.diagnosis.isNotEmpty) _line(Icons.search_rounded, '${tr('التشخيص', 'Diagnosis')}: ${v.diagnosis}'),
        if (v.treatment.isNotEmpty) _line(Icons.healing_rounded, '${tr('العلاج', 'Treatment')}: ${v.treatment}'),
        for (final (medicine, dose) in v.medications)
          _line(Icons.medication_rounded, [medicine, dose].where((s) => s.isNotEmpty).join(' — ')),
        if (v.notes.isNotEmpty) _line(Icons.sticky_note_2_outlined, v.notes),
        if (v.parentNotified)
          _line(Icons.notifications_active_outlined, tr('تم إبلاغ ولي الأمر', 'Parent was notified'), color: _green),
      ],
    );
  }

  // ----- Shared pieces -----

  /// "12 نوفمبر 2026"
  String _date(DateTime d) => '${d.day} ${monthNames[d.month - 1]} ${d.year}';

  Widget _iconBubble(IconData icon, Color color) => Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(color: color.withValues(alpha: .12), shape: BoxShape.circle),
        child: Icon(icon, color: color, size: 20),
      );

  Widget _card({
    required IconData icon,
    required Color color,
    required String title,
    required String subtitle,
    (String, Color)? badge,
    List<Widget> children = const [],
  }) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: context.palette.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _iconBubble(icon, color),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: context.palette.ink)),
                    const SizedBox(height: 3),
                    Text(subtitle, style: TextStyle(fontSize: 12, color: context.palette.muted)),
                  ],
                ),
              ),
              if (badge != null) ...[
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(color: badge.$2.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                  child: Text(badge.$1, style: TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: badge.$2)),
                ),
              ],
            ],
          ),
          if (children.isNotEmpty) ...[
            const SizedBox(height: 10),
            Divider(height: 1, color: context.palette.divider),
            const SizedBox(height: 8),
            ...children,
          ],
        ],
      ),
    );
  }

  Widget _line(IconData icon, String text, {Color? color}) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 3),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, size: 16, color: color ?? context.palette.faint),
            const SizedBox(width: 8),
            Expanded(
              child: Text(text, style: TextStyle(fontSize: 13, height: 1.4, color: color ?? context.palette.ink)),
            ),
          ],
        ),
      );

  Widget _message(IconData icon, String text, {(String, Future<void> Function())? action}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: context.palette.faint),
            const SizedBox(height: 12),
            Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.muted, height: 1.5)),
            if (action != null) ...[
              const SizedBox(height: 16),
              FilledButton(onPressed: action.$2, child: Text(action.$1)),
            ],
          ],
        ),
      ),
    );
  }
}

/// Form to add or edit the child's medical profile; pops with true when it was saved
class EditMedicalProfileScreen extends StatefulWidget {
  const EditMedicalProfileScreen({
    super.key,
    required this.child,
    required this.profile,
    this.saveProfile,
    this.onSessionExpired,
  });

  final Child child;
  final MedicalProfile profile;
  final SaveMedicalProfile? saveProfile;
  final VoidCallback? onSessionExpired;

  @override
  State<EditMedicalProfileScreen> createState() => _EditMedicalProfileScreenState();
}

class _EditMedicalProfileScreenState extends State<EditMedicalProfileScreen> {
  static const _bloodTypes = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];

  final _form = GlobalKey<FormState>();
  late String _bloodType = widget.profile.bloodType;
  late final _allergies = TextEditingController(text: widget.profile.allergies);
  late final _chronic = TextEditingController(text: widget.profile.chronicDiseases);
  late final _medications = TextEditingController(text: widget.profile.regularMedications);
  late final _phone = TextEditingController(text: widget.profile.emergencyPhone);
  late final _notes = TextEditingController(text: widget.profile.notes);
  bool _saving = false;

  @override
  void dispose() {
    for (final c in [_allergies, _chronic, _medications, _phone, _notes]) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!(_form.currentState?.validate() ?? false)) return;

    setState(() => _saving = true);
    try {
      final save = widget.saveProfile ?? ClinicService.instance.saveMedicalProfile;
      await save(
        widget.child.id,
        MedicalProfile(
          bloodType: _bloodType,
          allergies: _allergies.text,
          chronicDiseases: _chronic.text,
          regularMedications: _medications.text,
          emergencyPhone: _phone.text,
          notes: _notes.text,
        ),
      );
      if (!mounted) return;
      _showMessage(tr('تم حفظ الملف الصحي.', 'Health profile saved.'));
      final navigator = Navigator.of(context);
      if (navigator.canPop()) navigator.pop(true);
    } on ClinicException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired && widget.onSessionExpired != null) {
        Navigator.of(context).pop(false);
        widget.onSessionExpired!();
        return;
      }
      _showMessage(e.message);
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }

  @override
  Widget build(BuildContext context) {
    // Keep a value the clinic typed that is not in the list (e.g. "Unknown")
    final bloodTypes = [
      ..._bloodTypes,
      if (_bloodType.isNotEmpty && !_bloodTypes.contains(_bloodType)) _bloodType,
    ];

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الملف الصحي', 'Health profile'), style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(widget.child.name, style: const TextStyle(fontSize: 12, color: Colors.white70)),
          ],
        ),
      ),
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 560),
            child: Form(
              key: _form,
              child: ListView(
                padding: const EdgeInsets.all(18),
                children: [
                  DropdownButtonFormField<String>(
                    initialValue: _bloodType,
                    decoration: InputDecoration(
                      labelText: tr('فصيلة الدم', 'Blood type'),
                      prefixIcon: const Icon(Icons.bloodtype_rounded, color: _red),
                    ),
                    items: [
                      DropdownMenuItem(value: '', child: Text(tr('غير محدد', 'Not set'))),
                      for (final t in bloodTypes)
                        DropdownMenuItem(value: t, child: Text(t, textDirection: TextDirection.ltr)),
                    ],
                    onChanged: _saving ? null : (v) => setState(() => _bloodType = v ?? ''),
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    controller: _phone,
                    enabled: !_saving,
                    keyboardType: TextInputType.phone,
                    textDirection: TextDirection.ltr,
                    inputFormatters: [
                      LengthLimitingTextInputFormatter(ClinicService.maxEmergencyPhone),
                      FilteringTextInputFormatter.allow(RegExp(r'[0-9+\-\s()]')),
                    ],
                    decoration: InputDecoration(
                      labelText: tr('هاتف الطوارئ', 'Emergency phone'),
                      prefixIcon: const Icon(Icons.phone_in_talk_rounded),
                    ),
                    validator: (v) {
                      final digits = (v ?? '').replaceAll(RegExp(r'\D'), '');
                      if ((v ?? '').trim().isNotEmpty && digits.length < 6) {
                        return tr('رقم الهاتف غير صحيح', 'Enter a valid phone number');
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 14),
                  _multiline(_allergies, tr('الحساسية', 'Allergies'), Icons.warning_amber_rounded,
                      tr('مثال: الفول السوداني، البنسلين', 'e.g. peanuts, penicillin')),
                  _multiline(_chronic, tr('الأمراض المزمنة', 'Chronic diseases'), Icons.monitor_heart_rounded,
                      tr('مثال: الربو، السكري', 'e.g. asthma, diabetes')),
                  _multiline(_medications, tr('الأدوية المنتظمة', 'Regular medications'), Icons.medication_rounded,
                      tr('اسم الدواء والجرعة والموعد', 'Name, dose and time')),
                  _multiline(_notes, tr('ملاحظات', 'Notes'), Icons.sticky_note_2_rounded,
                      tr('أي معلومات أخرى تهم العيادة', 'Anything else the clinic should know')),
                  const SizedBox(height: 6),
                  FilledButton.icon(
                    onPressed: _saving ? null : _save,
                    icon: _saving
                        ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : const Icon(Icons.save_rounded),
                    label: Text(tr('حفظ', 'Save')),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _multiline(TextEditingController controller, String label, IconData icon, String hint) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: TextFormField(
        controller: controller,
        enabled: !_saving,
        minLines: 1,
        maxLines: 4,
        textInputAction: TextInputAction.newline,
        decoration: InputDecoration(labelText: label, hintText: hint, prefixIcon: Icon(icon)),
      ),
    );
  }
}
