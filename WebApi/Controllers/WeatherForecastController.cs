using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using WebApi.Models;

namespace WebApi.Controllers
{
    // see https://github.com/seble-nigussie/Neo4j-NET-core on tips and tricks
    //https://neo4j.com/docs/getting-started/cypher-intro/results/ Cypher docs.
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        private readonly ILogger<WeatherForecastController> _logger;
        private BoltGraphClient _client;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _client = new BoltGraphClient(new Uri("bolt://localhost:7687"), "neo4j", "password");
            _client.ConnectAsync().Wait();
        }

        [HttpGet]
        public async Task<Student> Get([FromQuery] int id)
        {
            var student = await _client.Cypher.Match("(s:Student)")
                .Where((Student s) => s.Id == id).Return(s => s.As<Student>()).ResultsAsync;
            return student.FirstOrDefault();
        }

        [HttpPost]
        public void Post([FromQuery] int id)
        {
            var student = new Student { Id = id, Description = "hej", Name = "med" };
            _client.Cypher.Create("(s:Student $student)").WithParam("student", student).ExecuteWithoutResultsAsync();
        }

        [HttpPost("lection")]
        public void PostLection([FromQuery] int id)
        {
            var lection = new Lection { Id = id, LectorName = "kenneth", LectionName = "Database" };

            _client.Cypher.Create("(l:Lection $lection)").WithParam("lection", lection).ExecuteWithoutResultsAsync();
        }
        [HttpGet("Assign")]
        public async Task Get([FromQuery] int ids, [FromQuery] int idl)
        {
            await _client.Cypher.Match("(l:Lection), (s:Student)")
                .Where((Lection l, Student s) => l.Id == idl && s.Id == ids)
                .Create("(l)-[r:hasStudents]->(s)").ExecuteWithoutResultsAsync();
        }

        [HttpGet("GetStudents")]
        public async Task<IEnumerable<Student>> GetStudentsFromLection([FromQuery] int idl)
        {
            //this gets all student which has the relation to idl
            var student = await _client.Cypher.Match("(l:Lection)-[r:hasStudents]->(s:Student)")
                .Where((Lection l) => l.Id == idl)
                .Return(s => s.As<Student>()).ResultsAsync; //you can return multiple object as.
                                                            //Add a comma on the return parameteren after As<Student>(),

            return student;
        }

        [HttpPost("lectionWithStudentRelation")]
        public void PostLectionWithStudentRelation([FromBody] Lection lection, [FromQuery] int ids)
        {
            _client.Cypher.Match("s:Student").Where((Student s, Lection l) => s.Id == ids && l.Id == lection.Id)
                .Merge("(l:Lection $lection)-[r:hasStudents]->(s:Student)").WithParam("lection", lection)
                .ExecuteWithoutResultsAsync();
        }

        [HttpPut("{id}")]
        public void UpdateStudent(int id, [FromBody] Student student)
        {
            _client.Cypher.Match("(s:Student)")
                               .Where((Student s) => s.Id == id)
                               .Set("s = $student")
                               .WithParam("student", student)
                               .ExecuteWithoutResultsAsync();
        }

        [HttpDelete("{id}")]
        public void DeleteStudent(int id)
        {
            _client.Cypher.Match("(s:Student)")
                                .Where((Student s) => s.Id == id)
                                .Delete("s")
                                .ExecuteWithoutResultsAsync();
        }

        [HttpDelete("StudentFromLection")]
        public void DeleteStudentFromLection(int ids,int idl)
        {
            _client.Cypher.Match("(l:Lection)-[r:hasStudents]->(s:Student)")
                                .Where((Student s,Lection l) => s.Id == ids && l.Id==idl)
                                .Delete("r")
                                .ExecuteWithoutResultsAsync();
        }
    }
}