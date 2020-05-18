using Microsoft.AspNetCore.Mvc;
using WebApplication.Entities;
using WebApplication.Models;
using WebApplication.Services;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentServiceDb _service;
        
        public StudentsController(IStudentServiceDb service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult GetStudents()
        {
            return Ok(_service.GetStudents());
        }
        
        [HttpPost("add")]
        public IActionResult InsertStudent(Student student)
        {
            var toReturn = _service.InsertStudent(student);
            if (toReturn == null)
            {
                return BadRequest();
            }
            return Ok(toReturn);
        }
        
        [HttpPost("update")]
        public IActionResult UpdateStudent(UpdateStudentRequest student)
        {
            var toReturn = _service.UpdateStudent(student);
            if (toReturn == null)
            {
                return BadRequest();
            }
            return Ok(toReturn);
        }
        
        [HttpDelete("delete")]
        public IActionResult DeleteStudent(string indexNumber)
        {
            if (_service.DeleteStudent(indexNumber))
            {
                return Ok();
            }
            return NotFound();
        }
    }
}