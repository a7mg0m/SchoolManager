using BuildingBlocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManager.ExamsHomework.Application;
using SchoolManager.ExamsHomework.Domain;

namespace SchoolManager.ExamsHomework.API.Controllers
{
    /// <summary>
    /// Teacher app: the quizzes and monthly exams of the subjects the logged-in teacher teaches, in the classes they
    /// teach them (their own and the ones by the school office or other teachers), and the students' scores.
    /// The teacher adds, edits and deletes only their own quizzes and monthly exams. The teacher id comes from the
    /// login token; what they teach is checked with the Management service. Parents see these exams and their
    /// child's score in the parent app.
    /// </summary>
    [Authorize]
    [Route("api/exam/my")]
    public class TeacherExamsController : ApiControllerBase
    {
        private static readonly ExamType[] AllowedTypes = { ExamType.Quiz, ExamType.Monthly };

        private readonly IMediator _mediator;
        private readonly IManagementClient _management;
        private readonly IExamRepository _exams;

        public TeacherExamsController(IMediator mediator, IManagementClient management, IExamRepository exams)
        {
            _mediator = mediator;
            _management = management;
            _exams = exams;
        }

        private string Authorization => Request.Headers.Authorization.ToString();

        /// <summary>
        /// The quizzes and monthly exams of the teacher's subjects and classes in the academic year / semester (null:
        /// all), newest first, with score counts: their own and the ones by the school office or other teachers
        /// (whole-grade exams too). Mid-term and final exams are not listed.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyExams([FromQuery] Guid? academicYearId, [FromQuery] Guid? semesterId, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var classes = await _management.GetMyClassesAsync(Authorization, academicYearId, semesterId, cancellationToken);
            if (classes is null)
            {
                // Management unavailable: the teacher's own exams only (names are optional here)
                var own = await _exams.GetForTeacherAsync(teacherId, academicYearId, semesterId, cancellationToken);
                var none = new TeacherClasses(null, null, Array.Empty<TeacherSubject>());
                return Ok(new ApiResponse
                {
                    data = own.Where(e => AllowedTypes.Contains(e.ExamType)).Select(e => ToDto(e, none, teacherId)).ToList()
                });
            }

            var subjectIds = classes.Subjects.Select(s => s.SubjectId).Distinct().ToList();
            var gradeIds = classes.Subjects.Select(s => s.GradeId).Distinct().ToList();
            var exams = await _exams.GetForTeacherClassesAsync(teacherId, subjectIds, gradeIds, academicYearId, semesterId, cancellationToken);
            return Ok(new ApiResponse
            {
                data = exams
                    .Where(e => AllowedTypes.Contains(e.ExamType) && (e.TeacherId == teacherId || Teaches(classes, e)))
                    .Select(e => ToDto(e, classes, teacherId))
                    .ToList()
            });
        }

        /// <summary>The exam is of a subject the teacher teaches in its grade, for the whole grade or one of their classes</summary>
        private static bool Teaches(TeacherClasses classes, Exam e)
        {
            var subject = classes.Subject(e.SubjectId, e.GradeId);
            return subject is not null && (e.SectionId is null || subject.Sections.Any(s => s.Id == e.SectionId));
        }

        /// <summary>
        /// The classes whose students the teacher scores in this exam: its class, or (whole-grade exam) the
        /// teacher's classes of the subject in that grade. Null when the teacher may not open it; empty list when
        /// the Management service could not answer.
        /// </summary>
        private async Task<List<Guid>?> ScoreSectionsAsync(Exam exam, Guid teacherId, CancellationToken cancellationToken)
        {
            if (exam.TeacherId == teacherId && exam.SectionId is Guid own)
                return new List<Guid> { own };

            var classes = await _management.GetMyClassesAsync(Authorization, exam.AcademicYearId, exam.SemesterId, cancellationToken);
            if (classes is null)
                return new List<Guid>();
            if (exam.TeacherId != teacherId && !Teaches(classes, exam))
                return null;
            if (exam.SectionId is Guid sectionId)
                return new List<Guid> { sectionId };
            return classes.Subject(exam.SubjectId, exam.GradeId)?.Sections.Select(s => s.Id).ToList() ?? new List<Guid>();
        }

        /// <summary>The students of the given classes, by name; null when the Management service could not answer</summary>
        private async Task<List<SectionStudent>?> StudentsOfAsync(List<Guid> sectionIds, CancellationToken cancellationToken)
        {
            var all = new List<SectionStudent>();
            foreach (var sectionId in sectionIds)
            {
                var students = await _management.GetMySectionStudentsAsync(Authorization, sectionId, cancellationToken);
                if (students is null)
                    return null;
                all.AddRange(students);
            }
            return all.GroupBy(s => s.Id).Select(g => g.First()).ToList();
        }

        /// <summary>New quiz / monthly exam for a class of a subject the teacher teaches.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateMyExam([FromBody] SaveMyExamDto dto, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));
            if (Validate(dto) is string error)
                return BadRequest(new ApiResponse(400, error));

            var classes = await _management.GetMyClassesAsync(Authorization, dto.AcademicYearId, dto.SemesterId, cancellationToken);
            if (classes is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            var subject = classes.Subject(dto.SubjectId, dto.GradeId);
            if (subject is null)
                return BadRequest(new ApiResponse(400, "SubjectNotTaught"));
            if (subject.Sections.All(s => s.Id != dto.SectionId))
                return BadRequest(new ApiResponse(400, "SectionNotTaught"));
            if (classes.AcademicYearId is not Guid yearId || classes.SemesterId is not Guid semesterId)
                return BadRequest(new ApiResponse(400, "NoActiveSemester"));

            var id = await _mediator.Send(new CreateExamCommand(
                dto.SubjectId, subject.StageId, dto.GradeId, dto.SectionId, yearId, semesterId,
                dto.ExamType, dto.ExamDate, dto.StartTime, dto.EndTime, Minutes(dto), dto.Weight, dto.SuccessScore,
                AnswerModelUrl: null, Month: dto.Month, TeacherId: teacherId), cancellationToken);
            return Ok(new ApiResponse { InsertedId = id.ToString() });
        }

        /// <summary>Edit one of the teacher's exams: type, month, class, date, times and marks (subject / grade stay).</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMyExam(Guid id, [FromBody] SaveMyExamDto dto, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var exam = await _exams.GetWithResultsAsync(id, cancellationToken);
            if (exam is null || exam.TeacherId != teacherId)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            if (Validate(dto) is string error)
                return BadRequest(new ApiResponse(400, error));
            // The full mark can't drop below scores already given
            if (exam.ExamResults.Any(r => r.Score > dto.Weight))
                return BadRequest(new ApiResponse(400, "WeightBelowExistingScores"));

            if (dto.SectionId != exam.SectionId)
            {
                // Another class of the same subject / grade, and only before any score was given
                if (exam.ExamResults.Count > 0)
                    return BadRequest(new ApiResponse(400, "SectionHasScores"));
                var classes = await _management.GetMyClassesAsync(Authorization, exam.AcademicYearId, exam.SemesterId, cancellationToken);
                if (classes is null)
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
                if (classes.Subject(exam.SubjectId, exam.GradeId)?.Sections.Any(s => s.Id == dto.SectionId) != true)
                    return BadRequest(new ApiResponse(400, "SectionNotTaught"));
            }

            await _mediator.Send(new UpdateExamCommand(
                id, exam.SubjectId, exam.StageId, exam.GradeId, dto.SectionId, exam.AcademicYearId, exam.SemesterId,
                dto.ExamType, dto.ExamDate, dto.StartTime, dto.EndTime, Minutes(dto), dto.Weight, dto.SuccessScore,
                exam.AnswerModelUrl, dto.Month), cancellationToken);
            return Ok(new ApiResponse { message = "Updated Successfully" });
        }

        /// <summary>Delete one of the teacher's exams with its scores.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMyExam(Guid id, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var exam = await _exams.GetWithResultsAsync(id, cancellationToken);
            if (exam is null || exam.TeacherId != teacherId)
                return NotFound(new ApiResponse(404, "ExamNotFound"));

            await _mediator.Send(new DeleteExamCommand(id), cancellationToken);
            return Ok(new ApiResponse { message = "Deleted Successfully" });
        }

        /// <summary>Students of the exam's class with their scores.</summary>
        [HttpGet("{id}/scores")]
        public async Task<IActionResult> GetScores(Guid id, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var exam = await _exams.GetWithResultsAsync(id, cancellationToken);
            if (exam is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            var sections = await ScoreSectionsAsync(exam, teacherId, cancellationToken);
            if (sections is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            var students = sections.Count == 0 ? null : await StudentsOfAsync(sections, cancellationToken);
            if (students is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            // A second final: only the students who failed its Final
            if (await SecondFinalStudentsAsync(exam, cancellationToken) is HashSet<Guid> eligible)
                students = students.Where(s => eligible.Contains(s.Id)).ToList();

            var scores = exam.ExamResults.GroupBy(r => r.StudentId).ToDictionary(g => g.Key, g => g.First().Score);
            return Ok(new ApiResponse
            {
                data = new TeacherExamScoresDto
                {
                    ExamId = exam.Id,
                    Weight = exam.Weight,
                    SuccessScore = exam.SuccessScore,
                    Students = students
                        .Select(s => new StudentExamScoreDto
                        {
                            StudentId = s.Id,
                            NameAr = s.NameAr,
                            NameEn = s.NameEn,
                            Score = scores.TryGetValue(s.Id, out var score) ? score : null
                        })
                        .ToList()
                }
            });
        }

        /// <summary>Save the scores of the class (a null score removes that student's score).</summary>
        [HttpPost("{id}/scores")]
        public async Task<IActionResult> SaveScores(Guid id, [FromBody] SaveMyExamScoresDto dto, CancellationToken cancellationToken)
        {
            if (CurrentTeacherId is not Guid teacherId)
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse(403, "NotATeacher"));

            var exam = await _exams.GetWithResultsAsync(id, cancellationToken);
            if (exam is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            var sections = await ScoreSectionsAsync(exam, teacherId, cancellationToken);
            if (sections is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));

            var results = dto?.Results ?? new List<StudentScoreDto>();
            if (results.GroupBy(r => r.StudentId).Any(g => g.Count() > 1))
                return BadRequest(new ApiResponse(400, "DuplicateStudent"));
            if (results.Any(r => r.Score is double s && (!double.IsFinite(s) || s < 0 || s > exam.Weight)))
                return BadRequest(new ApiResponse(400, "ScoreInvalid"));

            // Only students of the exam's class (whole-grade exam: of the teacher's classes); scores of other
            // students stay as they are
            var students = sections.Count == 0 ? null : await StudentsOfAsync(sections, cancellationToken);
            if (students is null)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse(503, "ManagementUnavailable"));
            var inClass = students.Select(s => s.Id).ToHashSet();
            if (results.Any(r => !inClass.Contains(r.StudentId)))
                return BadRequest(new ApiResponse(400, "StudentNotInClass"));
            if (await SecondFinalStudentsAsync(exam, cancellationToken) is HashSet<Guid> eligible
                && results.Any(r => r.Score is not null && !eligible.Contains(r.StudentId)))
                return BadRequest(new ApiResponse(400, SecondFinal.NotEligible));

            var summary = await _mediator.Send(new SaveExamResultsCommand(id, results), cancellationToken);
            if (summary is null)
                return NotFound(new ApiResponse(404, "ExamNotFound"));
            return Ok(new ApiResponse { message = "Saved Successfully", data = summary });
        }

        /// <summary>For a second final: the students who failed its Final; null for the other exams</summary>
        private async Task<HashSet<Guid>?> SecondFinalStudentsAsync(Exam exam, CancellationToken cancellationToken)
        {
            if (exam.ExamType != ExamType.SecondFinal) return null;
            var original = exam.OriginalExamId is Guid id ? await _exams.GetWithResultsAsync(id, cancellationToken) : null;
            return original is null ? new HashSet<Guid>() : SecondFinal.FailedStudents(original).Keys.ToHashSet();
        }

        /// <summary>Type (quiz / monthly), month, times and marks</summary>
        private static string? Validate(SaveMyExamDto? dto)
        {
            if (dto is null) return "ExamRequired";
            if (!AllowedTypes.Contains(dto.ExamType)) return "ExamTypeNotAllowed";
            if (dto.SectionId == Guid.Empty) return "SectionRequired";
            if (dto.ExamType == ExamType.Monthly)
            {
                if (dto.Month is null) return "MonthRequired";
                if (dto.Month is < 1 or > 12) return "MonthInvalid";
            }
            if (dto.EndTime <= dto.StartTime) return "EndTimeBeforeStart";
            if (!double.IsFinite(dto.Weight) || dto.Weight <= 0) return "WeightInvalid";
            if (!double.IsFinite(dto.SuccessScore) || dto.SuccessScore < 0 || dto.SuccessScore > dto.Weight) return "SuccessScoreInvalid";
            return null;
        }

        private static double Minutes(SaveMyExamDto dto) => (dto.EndTime - dto.StartTime).TotalMinutes;

        private static TeacherExamDto ToDto(Exam e, TeacherClasses classes, Guid teacherId)
        {
            var subject = classes.Subject(e.SubjectId, e.GradeId);
            var section = subject?.Sections.FirstOrDefault(s => s.Id == e.SectionId);
            return new TeacherExamDto
            {
                Id = e.Id,
                IsMine = e.TeacherId == teacherId,
                AddedByTeacher = e.TeacherId is not null,
                SubjectId = e.SubjectId,
                SubjectNameAr = subject?.SubjectNameAr ?? string.Empty,
                SubjectNameEn = subject?.SubjectNameEn ?? string.Empty,
                GradeId = e.GradeId,
                GradeNameAr = subject?.GradeNameAr ?? string.Empty,
                GradeNameEn = subject?.GradeNameEn ?? string.Empty,
                SectionId = e.SectionId,
                SectionNameAr = section?.NameAr ?? string.Empty,
                SectionNameEn = section?.NameEn ?? string.Empty,
                AcademicYearId = e.AcademicYearId,
                SemesterId = e.SemesterId,
                ExamType = e.ExamType,
                Month = e.Month,
                ExamDate = e.ExamDate,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                Duration = e.Duration,
                Weight = e.Weight,
                SuccessScore = e.SuccessScore,
                AnswerModelUrl = e.AnswerModelUrl,
                ResultCount = e.ExamResults.Count,
                AverageScore = e.ExamResults.Count == 0 ? null : Math.Round(e.ExamResults.Average(r => r.Score), 2)
            };
        }
    }
}
