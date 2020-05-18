using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebApplication.Entities;
using WebApplication.Models;

namespace WebApplication.Services
{
    public class SqlServerStudentDbService : IStudentServiceDb
    {
        private readonly StudentContext _studentContext;
        private const string ConnectionString = "ConnectionString";

        public SqlServerStudentDbService(StudentContext studentContext)
        {
            _studentContext = studentContext;
        }
        public EnrollStudentResponse EnrollStudent(EnrollStudentRequest request)
        {
            var studies = _studentContext.Studies.FirstOrDefault(s => s.Name == request.Studies);
            if (studies == null)
            {
                return null;
            }

            var enrollment = _studentContext.Enrollment.FirstOrDefault(e => e.Semester == 1 && e.IdStudy == studies.IdStudy);
            if (enrollment == null)
            {
                _studentContext.Enrollment.Add(new Enrollment()
                {
                    Semester = 1,
                    IdStudy = studies.IdStudy,
                    StartDate = DateTime.Today
                });
                if (_studentContext.SaveChanges() != 1)
                {
                    return null;
                }
                enrollment = _studentContext.Enrollment.FirstOrDefault(e => e.Semester == 1 && e.IdStudy == studies.IdStudy);
            }

            var student = new Student()
            {
                IndexNumber = request.IndexNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                BirthDate = request.BirthDate,
                IdEnrollment = enrollment.IdEnrollment
            };

            _studentContext.Student.Add(student);
            
            if (_studentContext.SaveChanges() == 1)
            {
                return new EnrollStudentResponse()
                {
                    LastName = request.LastName,
                    Semester = 1
                };
            }

            return null;
        }

        public PromoteStudentsResponse PromoteStudents(PromoteStudentsRequest request)
        {
            var studies = _studentContext.Studies.FirstOrDefault(s => s.Name == request.Studies);
            if (studies == null)
            {
                return null;
            }

            var oldEnrollmentId = _studentContext.Enrollment.FirstOrDefault(e => e.IdStudy == studies.IdStudy && e.Semester == request.Semester);
            if (oldEnrollmentId == null)
            {
                return null;
            }
            
            var enrollment = _studentContext.Enrollment.FirstOrDefault(e => e.IdStudy == studies.IdStudy && e.Semester == request.Semester+1);
            if (enrollment == null)
            {
                _studentContext.Enrollment.Add(new Enrollment()
                {
                    IdStudy = studies.IdStudy,
                    Semester = request.Semester+1,
                    StartDate = DateTime.Today
                });
                if (_studentContext.SaveChanges() != 1)
                {
                    return null;
                }
                enrollment = _studentContext.Enrollment.FirstOrDefault(e => e.IdStudy == studies.IdStudy && e.Semester == request.Semester+1);
            }
            
            var students = _studentContext.Student.Where(s => s.IdEnrollment == oldEnrollmentId.IdEnrollment).ToList();
            foreach (var s in students)
            {
                s.IdEnrollment = enrollment.IdEnrollment;
            }
            if (_studentContext.SaveChanges() != 1)
            {
                return null;
            }

            return new PromoteStudentsResponse()
            {
                IdEnrollment = enrollment.IdEnrollment,
                Semester = enrollment.Semester,
                StartDate = enrollment.StartDate,
                Study = studies.Name
            };
        }

        public void InsertRefreshToken(string indexNumber, string token)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
             {
                 connection.Open();
                 SqlCommand command = connection.CreateCommand();
                 SqlTransaction transaction = connection.BeginTransaction("TokenTransaction");
                 command.Connection = connection;
                 command.Transaction = transaction;

                 try
                 {
                     command.CommandText = "UPDATE Student SET RefreshToken=@RefreshToken WHERE IndexNumber=@UserIndex";
                     command.Parameters.AddWithValue("UserIndex", indexNumber);
                     command.Parameters.AddWithValue("RefreshToken", token);
                     command.ExecuteNonQuery();
                     transaction.Commit();
                 }
                 catch (Exception)
                 {
                     transaction.Rollback();
                 }
             }
        }

        public LoginResponse LoginRequest(string Login, string Password)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT IndexNumber, FirstName, UserRole FROM Student WHERE IndexNumber=@login AND Password=@password", connection);
                command.Parameters.AddWithValue("login", Login);
                command.Parameters.AddWithValue("password", Password);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new LoginResponse()
                        {
                            IndexNumber = reader["IndexNumber"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            Role = reader["UserRole"].ToString()
                        };
                    }
                }
                return null;
            }
        }

        public LoginResponse LoginRequest(string refreshToken)
        {
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand("SELECT IndexNumber, FirstName, UserRole FROM Student WHERE RefreshToken=@token", connection);
                command.Parameters.AddWithValue("token", refreshToken);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new LoginResponse()
                        {
                            IndexNumber = reader["IndexNumber"].ToString(),
                            FirstName = reader["FirstName"].ToString(),
                            Role = reader["UserRole"].ToString()
                        };
                    }
                }
                return null;
            }
        }

        public List<GetStudentsResponse> GetStudents()
        {
            var students = _studentContext.Student
                .Include(s => s.IdEnrollmentNavigation).ThenInclude(e => e.IdStudyNavigation)
                .Select(s => new GetStudentsResponse
                {
                    IndexNumber = s.IndexNumber,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    BirthDate = s.BirthDate.ToShortDateString(),
                    Semester = s.IdEnrollmentNavigation.Semester,
                    Studies = s.IdEnrollmentNavigation.IdStudyNavigation.Name
                })
                .ToList();

            return students;
        }

        public Student InsertStudent(Student request)
        {
            _studentContext.Student.Add(request);
            return _studentContext.SaveChanges() == 1 ? _studentContext.Student.First(s => s.IndexNumber == request.IndexNumber) : null;
        }

        public UpdateStudentResponse UpdateStudent(UpdateStudentRequest request)
        {
            var student = _studentContext.Student.First(s => s.IndexNumber == request.IndexNumber);
            
            if (request.FirstName != null)
            {
                student.FirstName = request.FirstName;
            }
            if (request.LastName != null)
            {
                student.LastName = request.LastName;
            }
            if (request.BirthDate != null)
            {
                student.BirthDate = request.BirthDate;
            }

            if (_studentContext.SaveChanges() != 1) return null;
            {
                var toReturn = _studentContext.Student.First(s => s.IndexNumber == request.IndexNumber);
                return new UpdateStudentResponse()
                {
                    IndexNumber = toReturn.IndexNumber,
                    FirstName = toReturn.FirstName,
                    LastName = toReturn.LastName,
                    BirthDate = toReturn.BirthDate
                };
            }
        }

        public bool DeleteStudent(string indexNumber)
        {
            var student = _studentContext.Student.First(s => s.IndexNumber == indexNumber);
            _studentContext.Student.Remove(student);
            return _studentContext.SaveChanges() == 1;
        }
    }
}