﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using Task = System.Threading.Tasks.Task;

var settings = ValidationSettings.CreateDefault();
IResourceResolver resolver = ZipSource.CreateValidationSource();

var multiResolver = new MultiResolver(resolver, new JsonFileResolver("StructureDefinition.json"));

settings.ResourceResolver = multiResolver;
settings.GenerateSnapshot = true;

// settings.ConstraintsToIgnore = new[] { "ref-1", "dom-6", "que-7" };

var jsonParser = new FhirJsonParser();
var carePlanFromFile = jsonParser.Parse<CarePlan>(File.ReadAllText("Data/HealthPlan.json"));
var validator = new Validator(settings);

var goal1 = new Goal
{
   Meta = new Meta
   {
      Profile = new List<string> { "http://ihis.sg/profile/HealthPlanGoal1" },
   },
   Id = "mygoal1",
   LifecycleStatus = Goal.GoalLifecycleStatus.Accepted,
   Description = new CodeableConcept { Text = "life goals" },
   Subject = new ResourceReference("Patient/abcd"),
   Target = new List<Goal.TargetComponent>
    {
        new Goal.TargetComponent
        {
            Measure = new CodeableConcept { Text = "life goals" },
            Detail = new Quantity(12, "d"),
        },
    },
};
var g42goal = new Goal
{
   Meta = new Meta
   {
      Profile = new List<string> { "http://ihis.sg/profile/G42" },
   },
   Id = "mygoal4",
   LifecycleStatus = Goal.GoalLifecycleStatus.Accepted,
   Description = new CodeableConcept { Text = "life goals" },
   Subject = new ResourceReference("Patient/abcd"),
   Target = new List<Goal.TargetComponent>
   {
      new Goal.TargetComponent
      {
         Measure = new CodeableConcept
         {
            Coding = new List<Coding>()
            {
               new Coding
               {
                  System = "http://ihis.sg/ValueSet/hsg-goal-measure",
                  Code = "GM004",
                  Display = "BP1"
               }
            },
            Text = "life goals"
         },
         Detail = new Ratio()
         {
            Numerator = new Quantity(12, "d"),
            Denominator = new Quantity(12, "d"),
         }
      },
      new Goal.TargetComponent
      {
         Measure = new CodeableConcept
         {
            Coding = new List<Coding>()
            {
               new Coding
               {
                  System = "http://ihis.sg/ValueSet/hsg-goal-measure",
                  Code = "GM005",
                  Display = "BP2"
               }
            },
            Text = "life goals"
         },
         Detail = new Quantity(12, "d"),
      }
   }
};
var carePlan = new CarePlan()
{
    Meta = new Meta
    {
        Profile = new List<string> { "http://ihis.sg/StructureDefinition/CarePlan-pophealth-plan" },
    },
    Status = RequestStatus.Active,
    Intent = CarePlan.CarePlanIntent.Plan,
    Subject = new ResourceReference("Patient/abcd"),
    Contained = new List<Resource>()
    {
       g42goal
    },
    Goal = new List<ResourceReference>()
    {
        //new ResourceReference()
        //{
        //    Reference = "#mygoal1"
        //},
        new ResourceReference()
        {
            Reference = "#mygoal4"
        }
    }
};

Console.WriteLine("Validating goal");
var g42result = validator.Validate(g42goal);
Console.WriteLine(g42result.ToJson(new FhirJsonSerializationSettings() { Pretty = true }));
Console.WriteLine("Validating careplan");
var result = validator.Validate(carePlan);
Console.WriteLine(result.ToJson(new FhirJsonSerializationSettings() { Pretty = true }));

class JsonFileResolver : IResourceResolver
{
    private readonly Bundle bundle;

    public JsonFileResolver(string jsonFile)
    {
        var jsonParser = new FhirJsonParser();
        bundle = jsonParser.Parse<Bundle>(File.ReadAllText(jsonFile));
    }

    public Resource ResolveByCanonicalUri(string uri)
    {
        var split = uri.Split('|');
        var res = bundle.Entry
            .SingleOrDefault(x =>
            {
                if (x.Resource is IVersionableConformanceResource ver && ver.Url != null)
                {
                    if (split.Length == 2 && ver.Version != null)
                    {
                        return ver.Url == split[0] && ver.Version == split[1];
                    }
                    else
                    {
                        return ver.Url == uri;
                    }
                }
                else
                {
                    return x.FullUrl == uri;
                }
            })?.Resource;
        return res;
    }

    public Task<Resource> ResolveByCanonicalUriAsync(string uri)
    {
        return Task.FromResult(ResolveByCanonicalUri(uri));
    }

    public Resource ResolveByUri(string uri)
    {
        var res = bundle.Entry.SingleOrDefault(x => x.FullUrl == uri)?.Resource;
        return res!;
    }

    public Task<Resource> ResolveByUriAsync(string uri)
    {
        return Task.FromResult(ResolveByUri(uri));
    }
}