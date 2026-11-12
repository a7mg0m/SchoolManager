import 'package:flutter_test/flutter_test.dart';

import 'package:school_manager_parent_app/app.dart';

void main() {
  testWidgets('Home screen shows welcome text', (tester) async {
    await tester.pumpWidget(const ParentApp());
    expect(find.text('Welcome, Parent'), findsOneWidget);
  });
}
