using System;
using System.Collections.Generic;
using WebApplication.Entities;
using WebApplication.Models;

namespace WebApplication.Services
{
    public interface IStudentServiceDb
    {
        EnrollStudentResponse EnrollStudent(EnrollStudentRequest request);
        PromoteStudentsResponse PromoteStudents(PromoteStudentsRequest request);
        void InsertRefreshToken(string indexNumber, string token);
        LoginResponse LoginRequest(string login, string password);
        LoginResponse LoginRequest(string refreshToken);
        List<GetStudentsResponse> GetStudents();
        Student InsertStudent(Student request);
        UpdateStudentResponse UpdateStudent(UpdateStudentRequest request);
        bool DeleteStudent(string indexNumber);
    }
}