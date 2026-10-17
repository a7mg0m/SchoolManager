using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolManager.Notification.Domain
{
    public enum EventForEnum
    {
        All = 1,          // جميع 
        Stage = 2,        // المرحلة
        Grade = 3,        // الصف
        Section = 4,        // الفصل
        Teacher = 5,      // المعلم
        Student = 6,      // الطالب
    }
}
