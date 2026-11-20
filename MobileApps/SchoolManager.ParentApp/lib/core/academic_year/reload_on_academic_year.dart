import 'package:flutter/widgets.dart';

import 'academic_year_controller.dart';

/// Reloads a page when another academic year is chosen (from the menu, while the page is open)
mixin ReloadOnAcademicYear<T extends StatefulWidget> on State<T> {
  /// Load the page's data again (for the newly chosen year)
  Future<void> reloadForAcademicYear();

  @override
  void initState() {
    super.initState();
    AcademicYearController.instance.addListener(_onAcademicYearChanged);
  }

  @override
  void dispose() {
    AcademicYearController.instance.removeListener(_onAcademicYearChanged);
    super.dispose();
  }

  void _onAcademicYearChanged() {
    if (mounted) reloadForAcademicYear();
  }
}
