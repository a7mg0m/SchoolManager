import 'package:flutter/material.dart';

import '../../core/academic_year/academic_year_controller.dart';
import '../../core/academic_year/academic_year_picker.dart';
import '../../core/academic_year/reload_on_academic_year.dart';
import '../../core/theme/app_theme.dart';
import '../children/children_service.dart';
import 'fees_service.dart';
import '../../core/i18n/app_language.dart';

/// Fees of the child selected on home: what is due, paid and still to pay, then each fee.
class FeesScreen extends StatefulWidget {
  const FeesScreen({super.key, required this.child, this.loadFees, this.onSessionExpired});

  final Child child;

  /// Defaults to the API (tests pass their own)
  final Future<List<StudentFeeItem>> Function(String studentId)? loadFees;

  /// Called when the token was rejected (the page closes first)
  final VoidCallback? onSessionExpired;

  @override
  State<FeesScreen> createState() => _FeesScreenState();
}

class _FeesScreenState extends State<FeesScreen> with ReloadOnAcademicYear {
  @override
  Future<void> reloadForAcademicYear() => _load();

  Color get _muted => context.palette.muted;
  static const _green = Color(0xFF13864B);
  static const _red = Color(0xFFD93025);
  static const _orange = Color(0xFFF08C00);
  Color get _grey => context.palette.faint;

  List<StudentFeeItem>? _fees;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = _fees == null;
      _error = null;
    });
    try {
      final loader = widget.loadFees ?? FeesService.instance.getChildFees;
      final result = await loader(widget.child.id);
      if (!mounted) return;
      // Still to pay first (soonest due date first), then the settled ones (newest first)
      final outstanding = result.where((f) => f.isOutstanding).toList()..sort((a, b) => a.dueDate.compareTo(b.dueDate));
      final settled = result.where((f) => !f.isOutstanding).toList()..sort((a, b) => b.dueDate.compareTo(a.dueDate));
      setState(() {
        _fees = [...outstanding, ...settled];
        _loading = false;
      });
    } on FeesException catch (e) {
      if (!mounted) return;
      if (e.sessionExpired) {
        Navigator.of(context).pop();
        widget.onSessionExpired?.call();
        return;
      }
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(tr('الرسوم الدراسية', 'School Fees'), style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            Text(
              [widget.child.name, widget.child.classInfo].where((s) => s.isNotEmpty).join(' · '),
              style: const TextStyle(fontSize: 12, color: Colors.white70),
            ),
            // Fees of the academic year chosen in the app
            if (AcademicYearController.instance.value != null)
              Text(AcademicYearController.instance.value!.name,
                  key: const ValueKey('fees-year'),
                  style: const TextStyle(fontSize: 11.5, color: Colors.white70, fontWeight: FontWeight.w600)),
          ],
        ),
        toolbarHeight: AcademicYearController.instance.value != null ? 70 : null,
        actions: [
          IconButton(
            key: const ValueKey('fees-change-year'),
            tooltip: tr('السنة الدراسية', 'Academic year'),
            icon: const Icon(Icons.date_range_rounded),
            onPressed: () => showAcademicYearSheet(context),
          ),
        ],
      ),
      body: SafeArea(
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? _centerMessage(Icons.error_outline_rounded, _error!, retry: true)
                : RefreshIndicator(onRefresh: _load, child: _content(_fees!)),
      ),
    );
  }

  Widget _content(List<StudentFeeItem> fees) {
    if (fees.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 80),
          _centerMessage(Icons.receipt_long_rounded, tr('لا توجد رسوم دراسية مسجلة للطالب.', 'No school fees recorded for this student.')),
        ],
      );
    }

    final outstanding = fees.where((f) => f.isOutstanding).toList();
    final settled = fees.where((f) => !f.isOutstanding).toList();

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
      children: [
        _summary(fees),
        const SizedBox(height: 20),
        if (outstanding.isNotEmpty) ...[
          _sectionTitle(tr('مستحقات قائمة', 'Outstanding fees'), outstanding.length),
          for (final fee in outstanding) _feeCard(fee),
          const SizedBox(height: 12),
        ],
        if (settled.isNotEmpty) ...[
          _sectionTitle(tr('رسوم مسددة أو معفاة', 'Paid or waived fees'), settled.length),
          for (final fee in settled) _feeCard(fee),
        ],
      ],
    );
  }

  /// Totals of all fees: due (after discount), settled, still to pay
  Widget _summary(List<StudentFeeItem> fees) {
    final currency = fees.first.currency;
    final net = fees.fold<double>(0, (s, f) => s + f.net);
    final paid = fees.fold<double>(0, (s, f) => s + f.paid);
    final waived = fees.fold<double>(0, (s, f) => s + f.waived);
    final balance = fees.fold<double>(0, (s, f) => s + f.balance);
    final overdue = fees.where((f) => f.effectiveStatus == FeeState.overdue).length;
    final ratio = net <= 0 ? 1.0 : ((paid + waived) / net).clamp(0, 1).toDouble();

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [Color(0xFF1677D2), Color(0xFF06468F)],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.circular(22),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text(tr('المتبقي للسداد', 'Left to pay'), style: TextStyle(color: Colors.white70, fontSize: 13)),
          const SizedBox(height: 4),
          Text(_money(balance, currency),
              style: const TextStyle(color: Colors.white, fontSize: 28, fontWeight: FontWeight.bold)),
          const SizedBox(height: 14),
          ClipRRect(
            borderRadius: BorderRadius.circular(6),
            child: LinearProgressIndicator(
              value: ratio,
              minHeight: 8,
              backgroundColor: Colors.white24,
              color: const Color(0xFF6EE7A8),
            ),
          ),
          const SizedBox(height: 6),
          Text(tr('تم سداد ${(ratio * 100).round()}% من الرسوم', '${(ratio * 100).round()}% of the fees paid'),
              style: const TextStyle(color: Colors.white70, fontSize: 12)),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(child: _summaryItem(tr('إجمالي الرسوم', 'Total fees'), _money(net, currency))),
              Container(width: 1, height: 34, color: Colors.white24),
              Expanded(child: _summaryItem(tr('المدفوع', 'Paid'), _money(paid, currency))),
              if (waived > 0) ...[
                Container(width: 1, height: 34, color: Colors.white24),
                Expanded(child: _summaryItem(tr('المعفى', 'Waived'), _money(waived, currency))),
              ],
            ],
          ),
          if (overdue > 0) ...[
            const SizedBox(height: 14),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(12)),
              child: Row(
                children: [
                  const Icon(Icons.warning_amber_rounded, color: _red, size: 20),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      overdue == 1 ? tr('يوجد رسم متأخر عن موعد السداد', 'One fee is past its due date') : tr('يوجد $overdue رسوم متأخرة عن موعد السداد', '$overdue fees are past their due date'),
                      style: const TextStyle(color: _red, fontWeight: FontWeight.bold, fontSize: 12.5),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _summaryItem(String label, String value) {
    return Column(
      children: [
        Text(label, style: const TextStyle(color: Colors.white70, fontSize: 11.5)),
        const SizedBox(height: 3),
        FittedBox(
          fit: BoxFit.scaleDown,
          child: Text(value, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14)),
        ),
      ],
    );
  }

  Widget _sectionTitle(String title, int count) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        children: [
          Text(title, style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.ink)),
          const SizedBox(width: 6),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 1),
            decoration: BoxDecoration(color: context.palette.chip, borderRadius: BorderRadius.circular(10)),
            child: Text('$count', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: _muted)),
          ),
        ],
      ),
    );
  }

  /// One fee: status, due date, amounts and how much of it is settled
  Widget _feeCard(StudentFeeItem fee) {
    final (label, color, icon) = _statusLook(fee.effectiveStatus);

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: fee.effectiveStatus == FeeState.overdue ? _red.withValues(alpha: .4) : context.palette.line),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(18),
        // IntrinsicHeight: the status strip stretches to the card's height inside the list
        child: IntrinsicHeight(
          child: Row(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(width: 5, color: color),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Status and due date (wraps on narrow screens)
                    Wrap(
                      alignment: WrapAlignment.spaceBetween,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      spacing: 8,
                      runSpacing: 6,
                      children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                          decoration: BoxDecoration(color: color.withValues(alpha: .12), borderRadius: BorderRadius.circular(20)),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(icon, size: 14, color: color),
                              const SizedBox(width: 4),
                              Text(label, style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: color)),
                            ],
                          ),
                        ),
                        Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.event_rounded, size: 15, color: _muted),
                            const SizedBox(width: 4),
                            Text(tr('الاستحقاق: ${_date(fee.dueDate)}', 'Due: ${_date(fee.dueDate)}'), style: TextStyle(fontSize: 12, color: _muted)),
                          ],
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    // Remaining (or settled) amount in large text
                    Text(
                      fee.isOutstanding ? tr('المتبقي ${_money(fee.balance, fee.currency)}', 'Left ${_money(fee.balance, fee.currency)}') : _money(fee.net, fee.currency),
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: fee.isOutstanding ? context.palette.ink : _grey,
                      ),
                    ),
                    const SizedBox(height: 10),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(6),
                      child: LinearProgressIndicator(
                        value: fee.settledRatio,
                        minHeight: 6,
                        backgroundColor: context.palette.chip,
                        color: fee.isOutstanding ? AppTheme.blue : _green,
                      ),
                    ),
                    const SizedBox(height: 12),
                    _amountRow(tr('قيمة الرسوم', 'Fee amount'), fee.total, fee.currency),
                    if (fee.discount > 0) _amountRow(tr('الخصم', 'Discount'), -fee.discount, fee.currency, color: _green),
                    _amountRow(tr('المدفوع', 'Paid'), fee.paid, fee.currency),
                    if (fee.waived > 0) _amountRow(tr('المعفى', 'Waived'), fee.waived, fee.currency, color: _green),
                    Divider(height: 16, color: context.palette.divider),
                    _amountRow(tr('المتبقي', 'Balance'), fee.balance, fee.currency, bold: true, color: fee.balance > 0 ? context.palette.ink : _green),
                    if (fee.waiveReason != null && fee.waiveReason!.isNotEmpty) ...[
                      const SizedBox(height: 8),
                      Text(tr('سبب الإعفاء: ${fee.waiveReason}', 'Waiver reason: ${fee.waiveReason}'), style: TextStyle(fontSize: 12, color: _muted)),
                    ],
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

  Widget _amountRow(String label, double amount, String currency, {bool bold = false, Color? color}) {
    final style = TextStyle(
      fontSize: 13,
      fontWeight: bold ? FontWeight.bold : FontWeight.normal,
      color: color ?? (bold ? context.palette.ink : _muted),
    );
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        children: [
          Expanded(child: Text(label, style: style)),
          Text(amount < 0 ? '- ${_money(-amount, currency)}' : _money(amount, currency), style: style),
        ],
      ),
    );
  }

  (String, Color, IconData) _statusLook(FeeState status) => switch (status) {
        FeeState.pending => (tr('بانتظار السداد', 'Awaiting payment'), AppTheme.blue, Icons.schedule_rounded),
        FeeState.partiallyPaid => (tr('مدفوع جزئياً', 'Partly paid'), _orange, Icons.timelapse_rounded),
        FeeState.paid => (tr('مدفوع بالكامل', 'Fully paid'), _green, Icons.check_circle_rounded),
        FeeState.waived => (tr('معفى', 'Waived'), _grey, Icons.volunteer_activism_rounded),
        FeeState.overdue => (tr('متأخر', 'Overdue'), _red, Icons.warning_amber_rounded),
      };

  Widget _centerMessage(IconData icon, String text, {bool retry = false}) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: retry ? _red : AppTheme.blue.withValues(alpha: .6)),
            const SizedBox(height: 12),
            Text(text, textAlign: TextAlign.center, style: TextStyle(color: context.palette.ink)),
            if (retry) ...[
              const SizedBox(height: 12),
              FilledButton(onPressed: _load, child: Text(tr('إعادة المحاولة', 'Try again'))),
            ],
          ],
        ),
      ),
    );
  }

  /// 1500 EGP -> "1,500 ج.م", 1500.5 -> "1,500.50 ج.م"
  static String _money(double amount, String currency) {
    final rounded = (amount * 100).round() / 100;
    final whole = rounded.truncate();
    final cents = ((rounded - whole).abs() * 100).round();
    final digits = whole.abs().toString().replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');
    final text = '${rounded < 0 ? '-' : ''}$digits${cents > 0 ? '.${cents.toString().padLeft(2, '0')}' : ''}';
    return '$text ${_currencyName(currency)}'.trim();
  }

  static String _currencyName(String code) => switch (code.toUpperCase()) {
        'EGP' => tr('ج.م', 'EGP'),
        'SAR' => tr('ر.س', 'SAR'),
        'AED' => tr('د.إ', 'AED'),
        'KWD' => tr('د.ك', 'KWD'),
        'USD' => r'$',
        _ => code,
      };

  /// 2026-11-20 -> "20/11/2026"
  static String _date(DateTime d) => '${d.day}/${d.month}/${d.year}';
}
