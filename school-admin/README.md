# قالب إدارة المدرسة (Angular 19)

## التشغيل
```bash
npm install
npm start
```
ثم افتح http://localhost:4200

## البنية
- `src/app/core/` النماذج وخدمة البيانات (SchoolService)
- `src/app/features/dashboard` الرئيسية
- `src/app/features/students` الطلاب (بحث، تصفية، إضافة مع التحقق، نقل)
- `src/app/features/classes` الفصول وإشغالها

## الربط بالخادم
البيانات الحالية في الذاكرة. استبدل الـ signals داخل `SchoolService` بطلبات HTTP عبر `HttpClient` عند ربط الـ API.
