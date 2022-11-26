// See https://aka.ms/new-console-template for more information
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;

var goal = new Goal
{
   LifecycleStatus = Goal.GoalLifecycleStatus.Accepted,
   Description = new CodeableConcept { Text = "life goals" },
   Subject = new ResourceReference("Patient/abcd"),
   Target = new List<Goal.TargetComponent>
   {
      new Goal.TargetComponent
      {
         Measure = new CodeableConcept { Text = "life goals" },
         Detail = new Duration { Value = 5m, System = "http://unitsofmeasure.org", Code = "d", Unit = "days" },
      },
   },
};


var settings = ValidationSettings.CreateDefault();
IResourceResolver resolver = ZipSource.CreateValidationSource();
settings.ResourceResolver = resolver;
settings.GenerateSnapshot = true;
settings.ConstraintsToIgnore = new[] { "ref-1", "dom-6", "que-7" };

var jsonParser = new FhirJsonParser();
var goalDefinition = jsonParser.Parse<StructureDefinition>(File.ReadAllText("TestGoal.StructureDefinition.json"));
var validator = new Validator(settings);

var result = validator.Validate(goal, goalDefinition);

Console.WriteLine(result.ToJson());