using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public enum NotificationTypeEnum
    {
        Homework = 1, // الواجب
        Exam = 2, // الامتحان
        ExamResult = 3, // نتيجة الامتحان
        Event = 4, // الحدث
        Message = 5 // الرسالة
    }

}
