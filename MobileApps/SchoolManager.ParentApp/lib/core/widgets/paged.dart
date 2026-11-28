import 'package:flutter/material.dart';

import '../i18n/app_language.dart';

/// Pages long lists on a screen: each list (by an id) shows its first [pageSize] items, then [pageSize] more each
/// time "Show more" is pressed. Only the shown items are built, so long lists (hundreds of exams, trips…) stay fast.
/// Counts and summaries keep using the whole list.
///
///   final _pages = PageWindows();
///   for (final e in _pages.take('exams', exams)) _examCard(e),
///   _pages.more(context, 'exams', exams.length, setState),
class PageWindows {
  PageWindows({this.pageSize = 20});

  final int pageSize;
  final _shown = <String, int>{};

  int _count(String id) => _shown[id] ?? pageSize;

  /// The items of [id] to show now
  List<T> take<T>(String id, List<T> items) {
    final n = _count(id);
    return items.length <= n ? items : items.sublist(0, n);
  }

  /// Back to the first page (e.g. after another filter)
  void reset([String? id]) => id == null ? _shown.clear() : _shown.remove(id);

  /// "Show more (N)" under the list while items are hidden; nothing otherwise
  Widget more(BuildContext context, String id, int total, void Function(VoidCallback) setState) {
    final left = total - _count(id);
    if (left <= 0) return const SizedBox.shrink();
    final next = left < pageSize ? left : pageSize;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Center(
        child: OutlinedButton.icon(
          key: ValueKey('show-more-$id'),
          onPressed: () => setState(() => _shown[id] = _count(id) + pageSize),
          icon: const Icon(Icons.expand_more_rounded),
          label: Text(tr('عرض المزيد ($next من $left)', 'Show more ($next of $left)')),
        ),
      ),
    );
  }
}
