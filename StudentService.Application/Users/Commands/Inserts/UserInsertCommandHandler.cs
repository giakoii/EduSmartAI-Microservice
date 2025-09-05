using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using Shared.Application.Interfaces.Repositories;
using Shared.Application.Utils.Const;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Users.Commands.Inserts;

public class UserInsertCommandHandler : ICommandHandler<UserInsertCommand, UserInsertEventResponse>
{
    private readonly ICommandRepository<Student> _studentRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly ICommandRepository<Teacher> _teacherRepository;
    private readonly IQueryRepository<TeacherCollection> _teacherCollectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentRepository"></param>
    /// <param name="teacherRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="studentQueryRepository"></param>
    /// <param name="teacherCollectionRepository"></param>
    public UserInsertCommandHandler(ICommandRepository<Student> studentRepository,
        ICommandRepository<Teacher> teacherRepository,
        IUnitOfWork unitOfWork, IQueryRepository<StudentCollection> studentQueryRepository,
        IQueryRepository<TeacherCollection> teacherCollectionRepository)
    {
        _studentRepository = studentRepository;
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
        _studentQueryRepository = studentQueryRepository;
        _teacherCollectionRepository = teacherCollectionRepository;
    }

    public async Task<UserInsertEventResponse> Handle(UserInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new UserInsertEventResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            switch (request.UserRole)
            {
                case (byte)ConstantEnum.UserRole.Student:
                {
                    // Insert new Student
                    var student = new Student
                    {
                        StudentId = request.UserId,
                        FirstName = request.FirstName,
                        LastName = request.LastName
                    };

                    await _studentRepository.AddAsync(student);

                    // If OldUserId is not null, delete the old student record
                    if (request.OldUserId.HasValue)
                    {
                        // Check if the old student exists
                        var oldStudent = await _studentRepository.FirstOrDefaultAsync(x => x.StudentId == request.OldUserId && x.IsActive == true, cancellationToken);
                        if (oldStudent != null)
                        {
                            _studentRepository.Update(oldStudent);
                            await _unitOfWork.SaveChangesAsync(request.Enail, cancellationToken, true);
                            
                            // Delete the associated StudentCollection if it exists
                            var oldStudentCollection = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == request.OldUserId && x.IsActive == true);
                            if (oldStudentCollection != null)
                            {
                                _unitOfWork.Delete(oldStudentCollection);
                                await _unitOfWork.SessionSaveChangesAsync();
                            }
                        }
                    }

                    await _unitOfWork.SaveChangesAsync(request.Enail, cancellationToken);
                    
                    // Insert into StudentCollection
                    var studentCollection = new StudentCollection
                    {
                        StudentId = request.UserId,
                        FirstName = request.FirstName,
                        LastName = request.LastName
                    };
                    _unitOfWork.Store(studentCollection);
                    await _unitOfWork.SessionSaveChangesAsync();
                    
                    break;
                }

                case (byte)ConstantEnum.UserRole.Lecturer:
                {
                    // Insert into Teacher
                    var teacher = new Teacher
                    {
                        TeacherId = request.UserId,
                        FirstName = request.FirstName,
                        LastName = request.LastName
                    };
                    await _teacherRepository.AddAsync(teacher);

                    // If OldUserId is not null, delete the old teacher record
                    if (request.OldUserId.HasValue)
                    {
                        // Check if the old teacher exists
                        var oldTeacher = await _teacherRepository.FirstOrDefaultAsync(x => x.TeacherId == request.OldUserId && x.IsActive == true, cancellationToken);
                        if (oldTeacher != null)
                        {
                            _teacherRepository.Update(oldTeacher);
                            await _unitOfWork.SaveChangesAsync(request.Enail, cancellationToken, true);
                            
                            // If the old teacher exists, we can also delete the associated TeacherCollection
                            var oldTeacherCollection = await _teacherCollectionRepository.FirstOrDefaultAsync(x => x.TeacherId == request.OldUserId && x.IsActive == true);
                            if (oldTeacherCollection != null)
                            {
                                _unitOfWork.Delete(oldTeacherCollection);
                                await _unitOfWork.SessionSaveChangesAsync();
                            }
                        }
                    }
                    
                    await _unitOfWork.SaveChangesAsync(request.Enail, cancellationToken);
                    
                    // Insert into TeacherCollection
                    var teacherCollection = new TeacherCollection
                    {
                        TeacherId = request.UserId,
                        FirstName = request.FirstName,
                        LastName = request.LastName
                    };
                    _unitOfWork.Store(teacherCollection);
                    await _unitOfWork.SessionSaveChangesAsync();

                    break;
                }
                default:
                    response.SetMessage(MessageId.E00000, "Sai thông tin vai trò người dùng");
                    return false;
            }

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);
        return response;
    }
}