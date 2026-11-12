import 'package:flutter/material.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('School Manager')),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.family_restroom, size: 72, color: theme.colorScheme.primary),
            const SizedBox(height: 16),
            Text('Welcome, Parent', style: theme.textTheme.headlineSmall),
          ],
        ),
      ),
    );
  }
}
