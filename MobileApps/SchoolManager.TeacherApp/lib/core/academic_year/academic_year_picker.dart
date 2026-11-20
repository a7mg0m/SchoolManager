import 'package:flutter/material.dart';

import '../i18n/app_language.dart';
import '../theme/app_theme.dart';
import 'academic_year_controller.dart';

InputDecoration _fieldDecoration(BuildContext context, String label) => InputDecoration(
      labelText: label,
      filled: true,
      fillColor: context.palette.field,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
    );

/// Academic year and semester dropdowns (menu): the current ones are marked; the choice is saved
class AcademicYearPicker extends StatelessWidget {
  const AcademicYearPicker({super.key});

  @override
  Widget build(BuildContext context) {
    final controller = AcademicYearController.instance;
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final years = controller.years;
        if (years.isEmpty) {
          return Text(tr('لا توجد سنوات دراسية.', 'No academic years.'), style: TextStyle(color: context.palette.muted));
        }
        final now = DateTime.now();
        final currentYear = AcademicYearController.currentOf(years, now);
        final year = controller.value;
        final semesters = year?.semesters ?? const <AcademicSemester>[];
        final currentSemester = AcademicYearController.currentSemesterOf(year, now);
        final current = tr('الحالي', 'Current');

        return Column(
          children: [
            DropdownButtonFormField<String>(
              key: const ValueKey('academic-year'),
              initialValue: year?.id,
              isExpanded: true,
              decoration: _fieldDecoration(context, tr('السنة', 'Year')),
              items: [
                for (final y in years)
                  DropdownMenuItem(
                    value: y.id,
                    child: Text(y.id == currentYear?.id ? '${y.name}  •  ${tr('الحالية', 'Current')}' : y.name,
                        overflow: TextOverflow.ellipsis),
                  ),
              ],
              onChanged: (id) {
                final chosen = years.where((y) => y.id == id).firstOrNull;
                if (chosen != null) controller.select(chosen);
              },
            ),
            if (semesters.isNotEmpty) ...[
              const SizedBox(height: 10),
              DropdownButtonFormField<String>(
                // A new key per year: the list of semesters changes with the year
                key: ValueKey('semester-${year?.id}'),
                initialValue: controller.selectedSemesterId,
                isExpanded: true,
                decoration: _fieldDecoration(context, tr('الفصل الدراسي', 'Semester')),
                items: [
                  for (final s in semesters)
                    DropdownMenuItem(
                      value: s.id,
                      child: Text(s.id == currentSemester?.id ? '${s.name}  •  $current' : s.name,
                          overflow: TextOverflow.ellipsis),
                    ),
                ],
                onChanged: (id) {
                  final chosen = semesters.where((s) => s.id == id).firstOrNull;
                  if (chosen != null) controller.selectSemester(chosen);
                },
              ),
            ],
          ],
        );
      },
    );
  }
}

/// Small chip with the chosen year and semester (header); tap to change them
class AcademicYearChip extends StatelessWidget {
  const AcademicYearChip({super.key, this.foreground = Colors.white});

  final Color foreground;

  @override
  Widget build(BuildContext context) {
    final controller = AcademicYearController.instance;
    return ListenableBuilder(
      listenable: controller,
      builder: (context, _) {
        final year = controller.value;
        if (year == null) return const SizedBox.shrink();
        final semester = controller.semester;
        return InkWell(
          key: const ValueKey('academic-year-chip'),
          borderRadius: BorderRadius.circular(20),
          onTap: () => showAcademicYearSheet(context),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: foreground.withValues(alpha: .14),
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: foreground.withValues(alpha: .18)),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.date_range_rounded, color: foreground, size: 15),
                const SizedBox(width: 6),
                Flexible(
                  child: Text(
                    semester == null ? year.name : '${year.name} · ${semester.name}',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(color: foreground, fontSize: 12.5, fontWeight: FontWeight.w600),
                  ),
                ),
                Icon(Icons.arrow_drop_down_rounded, color: foreground, size: 18),
              ],
            ),
          ),
        );
      },
    );
  }
}

/// Bottom sheet: the years, then the semesters of the chosen year; tap to choose
Future<void> showAcademicYearSheet(BuildContext context) {
  final controller = AcademicYearController.instance;
  return showModalBottomSheet<void>(
    context: context,
    showDragHandle: true,
    isScrollControlled: true,
    builder: (sheetContext) => ListenableBuilder(
      listenable: controller,
      builder: (sheetContext, _) {
        final now = DateTime.now();
        final currentYear = AcademicYearController.currentOf(controller.years, now);
        final year = controller.value;
        final currentSemester = AcademicYearController.currentSemesterOf(year, now);
        Widget heading(String text) => Padding(
              padding: const EdgeInsets.fromLTRB(18, 8, 18, 4),
              child: Align(
                alignment: AlignmentDirectional.centerStart,
                child: Text(text,
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: sheetContext.palette.ink)),
              ),
            );
        Icon mark(bool selected) => Icon(
              selected ? Icons.radio_button_checked_rounded : Icons.radio_button_off_rounded,
              color: AppTheme.blue,
            );

        return SafeArea(
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                heading(tr('السنة الدراسية', 'Academic year')),
                for (final y in controller.years)
                  ListTile(
                    leading: mark(y.id == controller.selectedId),
                    title: Text(y.name),
                    subtitle: y.id == currentYear?.id ? Text(tr('السنة الحالية', 'Current year')) : null,
                    onTap: () => controller.select(y),
                  ),
                if (year != null && year.semesters.isNotEmpty) ...[
                  const Divider(),
                  heading(tr('الفصل الدراسي', 'Semester')),
                  for (final s in year.semesters)
                    ListTile(
                      leading: mark(s.id == controller.selectedSemesterId),
                      title: Text(s.name),
                      subtitle: s.id == currentSemester?.id ? Text(tr('الفصل الحالي', 'Current semester')) : null,
                      onTap: () {
                        controller.selectSemester(s);
                        Navigator.of(sheetContext).pop();
                      },
                    ),
                ],
                const SizedBox(height: 8),
              ],
            ),
          ),
        );
      },
    ),
  );
}
