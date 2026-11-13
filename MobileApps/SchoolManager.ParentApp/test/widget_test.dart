import 'package:flutter_test/flutter_test.dart';

import 'package:school_manager_parent_app/app.dart';

void main() {
  testWidgets('App starts on the login screen', (tester) async {
    await tester.pumpWidget(const ParentApp());
    expect(find.text('Sign in to continue'), findsOneWidget);
    expect(find.text('Login'), findsOneWidget);
  });
}
