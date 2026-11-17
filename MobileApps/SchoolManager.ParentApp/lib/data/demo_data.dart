class Student {
  const Student({
    required this.id,
    required this.name,
    required this.grade,
    required this.className,
    required this.attendance,
    required this.pendingHomework,
    required this.average,
    required this.avatar,
  });

  final String id;
  final String name;
  final String grade;
  final String className;
  final int attendance;
  final int pendingHomework;
  final int average;
  final String avatar;
}

const demoStudents = <Student>[
  Student(
    id: '1',
    name: 'محمد أحمد جمعة',
    grade: 'الصف الأول الابتدائي',
    className: 'الفصل A',
    attendance: 92,
    pendingHomework: 2,
    average: 95,
    avatar: '👦🏻',
  ),
  Student(
    id: '2',
    name: 'عمر أحمد جمعة',
    grade: 'رياض الأطفال KG 2',
    className: 'الفصل B',
    attendance: 96,
    pendingHomework: 1,
    average: 98,
    avatar: '🧒🏻',
  ),
];

const parentName = 'أحمد جمعة';
