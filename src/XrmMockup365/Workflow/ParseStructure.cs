using System.Xml.Serialization;

namespace WorkflowParser {

    [XmlRoot("Activity")]
    public class ActivityRoot {
        [XmlElement("Members")]
        public Members Members;

        [XmlElement("Workflow")]
        public Workflow Workflow;
    }

    [XmlType("Members")]
    public class Members {
        [XmlElement("Property")]
        public Property[] Properties;
    }

    [XmlType("Property")]
    public class Property {

        [XmlAttribute("Name")]
        public string Name;

        [XmlAttribute("Type")]
        public string Type;
                
        [XmlArray("Property.Attributes")]
        [XmlArrayItem("ArgumentRequiredAttribute", Type = typeof(ArgumentRequiredAttribute)),
        XmlArrayItem("ArgumentTargetAttribute", Type = typeof(ArgumentTargetAttribute)),
        XmlArrayItem("ArgumentDescriptionAttribute", Type = typeof(ArgumentDescriptionAttribute)),
        XmlArrayItem("ArgumentDirectionAttribute", Type = typeof(ArgumentDirectionAttribute)),
        XmlArrayItem("ArgumentEntityAttribute", Type = typeof(ArgumentEntityAttribute))]
        public PropertyAttribute[] Attributes;
    }

    [XmlInclude(typeof(ArgumentRequiredAttribute)),
    XmlInclude(typeof(ArgumentTargetAttribute)),
    XmlInclude(typeof(ArgumentDescriptionAttribute)),
    XmlInclude(typeof(ArgumentDirectionAttribute)),
    XmlInclude(typeof(ArgumentEntityAttribute))]
    public class PropertyAttribute {
        [XmlAttribute("Value")]
        public string Value;
    }

    [XmlType("ArgumentRequiredAttribute")]
    public class ArgumentRequiredAttribute : PropertyAttribute { }

    [XmlType("ArgumentTargetAttribute")]
    public class ArgumentTargetAttribute : PropertyAttribute { }

    [XmlType("ArgumentDescriptionAttribute")]
    public class ArgumentDescriptionAttribute : PropertyAttribute { }

    [XmlType("ArgumentDirectionAttribute")]
    public class ArgumentDirectionAttribute : PropertyAttribute { }

    [XmlType("ArgumentEntityAttribute")]
    public class ArgumentEntityAttribute : PropertyAttribute { }

    public class Workflow {

        [XmlElement("ActivityReference", Type = typeof(ActivityReference))]
        [XmlElement("GetEntityProperty", Type = typeof(GetEntityProperty))]
        [XmlElement("TerminateWorkflow", Type = typeof(TerminateWorkflow))]
        [XmlElement("SetEntityProperty", Type = typeof(SetEntityProperty))]
        [XmlElement("CreateEntity", Type = typeof(CreateEntity))]
        [XmlElement("UpdateEntity", Type = typeof(UpdateEntity))]
        [XmlElement("AssignEntity", Type = typeof(AssignEntity))]
        [XmlElement("Assign", Type = typeof(Assign))]
        [XmlElement("SetAttributeValue", Type = typeof(SetAttributeValue))]
        [XmlElement("SetState", Type = typeof(SetState))]
        [XmlElement("Sequence", Type = typeof(ActivitySequence))]
        [XmlElement("Postpone", Type = typeof(Postpone))]
        [XmlElement("Null", Type = typeof(Null))]
        [XmlElement("Collection", Type = typeof(Collection))]
        [XmlElement("Persist", Type = typeof(Persist))]
        [XmlElement("SendEmail", Type = typeof(SendEmail))]
        public Activity[] Activities;
    }

    public abstract class Activity {
        [XmlAttribute("Key")]
        public string Key;
    }

    [XmlType("ActivityReference")]
    public class ActivityReference : Activity {

        [XmlAttribute("AssemblyQualifiedName")]
        public string AssemblyQualifiedName;

        [XmlArray("ActivityReference.Arguments")]
        [XmlArrayItem("InArgument", Type = typeof(InArgument)),
        XmlArrayItem("OutArgument", Type = typeof(OutArgument))]
        public Argument[] Arguments;


        [XmlArray("ActivityReference.Properties")]
        [XmlArrayItem("ActivityReference", Type = typeof(ActivityReference)),
        XmlArrayItem("GetEntityProperty", Type = typeof(GetEntityProperty)),
        XmlArrayItem("TerminateWorkflow", Type = typeof(TerminateWorkflow)),
        XmlArrayItem("SetEntityProperty", Type = typeof(SetEntityProperty)),
        XmlArrayItem("CreateEntity", Type = typeof(CreateEntity)),
        XmlArrayItem("UpdateEntity", Type = typeof(UpdateEntity)),
        XmlArrayItem("AssignEntity", Type = typeof(AssignEntity)),
        XmlArrayItem("Assign", Type = typeof(Assign)),
        XmlArrayItem("SetAttributeValue", Type = typeof(SetAttributeValue)),
        XmlArrayItem("SetState", Type = typeof(SetState)),
        XmlArrayItem("Sequence", Type = typeof(ActivitySequence)),
        XmlArrayItem("Postpone", Type = typeof(Postpone)),
        XmlArrayItem("Null", Type = typeof(Null)),
        XmlArrayItem("Collection", Type = typeof(Collection)),
        XmlArrayItem("Persist", Type = typeof(Persist)),
        XmlArrayItem("SendEmail", Type = typeof(SendEmail))]
        public Activity[] Properties;
    }

    public class ReferenceLiteral {
        [XmlAttribute("TypeArguments")]
        public string Type;

        [XmlAttribute("Value")]
        public string Value;

        [XmlElement("OptionSetValue")]
        public OptionSetValue OptionSetValue;
    }

    public class OptionSetValue {
        [XmlAttribute("Value")]
        public int Value;
    }

    [XmlInclude(typeof(InArgument)), XmlInclude(typeof(OutArgument))]
    public class Argument {
        [XmlAttribute("TypeArguments")]
        public string Type;

        [XmlAttribute("Key")]
        public string Key;

        [XmlText]
        public string Value;

        [XmlElement("ReferenceLiteral")]
        public ReferenceLiteral ReferenceLiteral;
    }

    [XmlType("InArgument")]
    public class InArgument : Argument {


    }

    [XmlType("OutArgument")]
    public class OutArgument : Argument {

    }


    public class Persist : Activity {

    }

    public class Collection : Activity {

        [XmlAttribute("TypeArguments")]
        public string Type;

        [XmlAttribute("Key")]
        public new string Key;

        [XmlElement("Variable")]
        public Variable[] Variables;

        [XmlElement("ActivityReference", Type = typeof(ActivityReference))]
        [XmlElement("GetEntityProperty", Type = typeof(GetEntityProperty))]
        [XmlElement("TerminateWorkflow", Type = typeof(TerminateWorkflow))]
        [XmlElement("SetEntityProperty", Type = typeof(SetEntityProperty))]
        [XmlElement("CreateEntity", Type = typeof(CreateEntity))]
        [XmlElement("UpdateEntity", Type = typeof(UpdateEntity))]
        [XmlElement("AssignEntity", Type = typeof(AssignEntity))]
        [XmlElement("Assign", Type = typeof(Assign))]
        [XmlElement("SetAttributeValue", Type = typeof(SetAttributeValue))]
        [XmlElement("SetState", Type = typeof(SetState))]
        [XmlElement("Sequence", Type = typeof(ActivitySequence))]
        [XmlElement("Postpone", Type = typeof(Postpone))]
        [XmlElement("Null", Type = typeof(Null))]
        [XmlElement("Collection", Type = typeof(Collection))]
        [XmlElement("Persist", Type = typeof(Persist))]
        [XmlElement("SendEmail", Type = typeof(SendEmail))]
        public Activity[] Activities;

    }

    public class ActivitySequence : Activity {

        [XmlArray("Sequence.Variables")]
        [XmlArrayItem("Variable")]
        public Variable[] Variables;

        [XmlElement("ActivityReference", Type = typeof(ActivityReference))]
        [XmlElement("GetEntityProperty", Type = typeof(GetEntityProperty))]
        [XmlElement("TerminateWorkflow", Type = typeof(TerminateWorkflow))]
        [XmlElement("SetEntityProperty", Type = typeof(SetEntityProperty))]
        [XmlElement("CreateEntity", Type = typeof(CreateEntity))]
        [XmlElement("UpdateEntity", Type = typeof(UpdateEntity))]
        [XmlElement("AssignEntity", Type = typeof(AssignEntity))]
        [XmlElement("Assign", Type = typeof(Assign))]
        [XmlElement("SetAttributeValue", Type = typeof(SetAttributeValue))]
        [XmlElement("SetState", Type = typeof(SetState))]
        [XmlElement("Sequence", Type = typeof(ActivitySequence))]
        [XmlElement("Postpone", Type = typeof(Postpone))]
        [XmlElement("Null", Type = typeof(Null))]
        [XmlElement("Collection", Type = typeof(Collection))]
        [XmlElement("Persist", Type = typeof(Persist))]
        [XmlElement("SendEmail", Type = typeof(SendEmail))]
        public Activity[] Activities;

    }

    [XmlType("TerminateWorkflow")]
    public class TerminateWorkflow : Activity {
        [XmlAttribute("Exception")]
        public string Exception;

        [XmlAttribute("Reason")]
        public string Reason;
    }

    [XmlType("SetEntityProperty")]
    public class SetEntityProperty : Activity {
        [XmlAttribute("Attribute")]
        public string Attribute;

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityName")]
        public string EntityName;

        [XmlAttribute("Value")]
        public string Value;

        [XmlArray("SetEntityProperty.TargetType")]
        [XmlArrayItem("InArgument")]
        public Argument[] InArguments;
    }

    [XmlType("CreateEntity")]
    public class CreateEntity : Activity {
        [XmlAttribute("EntityId")]
        public string EntityId;

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityName")]
        public string EntityName;
    }

    [XmlType("UpdateEntity")]
    public class UpdateEntity : Activity {

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityName")]
        public string EntityName;
    }

    [XmlType("AssignEntity")]
    public class AssignEntity : Activity {

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityId")]
        public string EntityId;

        [XmlAttribute("EntityName")]
        public string EntityName;

        [XmlAttribute("Owner")]
        public string Owner;
    }

    [XmlType("SetState")]
    public class SetState : Activity {

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityId")]
        public string EntityId;

        [XmlAttribute("EntityName")]
        public string EntityName;

        [XmlArray("SetState.State")]
        [XmlArrayItem("InArgument")]
        public InArgument[] StateArguments;

        [XmlArray("SetState.Status")]
        [XmlArrayItem("InArgument")]
        public InArgument[] StatusArguments;


    }

    [XmlType("SetAttributeValue")]
    public class SetAttributeValue : Activity {

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityName")]
        public string EntityName;
    }

    public class Variable {

        [XmlAttribute("TypeArguments")]
        public string Type;

        [XmlAttribute("Name")]
        public string Name;

        [XmlAttribute("Default")]
        public string Default;

        [XmlArray("Variable.Default")]
        [XmlArrayItem("Literal")]
        public Literal[] DefaultList;

    }

    public class Literal {
        [XmlAttribute("TypeArguments")]
        public string Type;

        [XmlAttribute("Value")]
        public string Value;

        [XmlElement("XrmTimeSpan")]
        public XrmTimeSpan XrmTimeSpan;
    }

    public class XrmTimeSpan {
        [XmlAttribute("Days")]
        public string Days;
        [XmlAttribute("Hours")]
        public string Hours;
        [XmlAttribute("Minutes")]
        public string Minutes;
        [XmlAttribute("Months")]
        public string Months;
        [XmlAttribute("Years")]
        public string Years;

    }

    [XmlType("GetEntityProperty")]
    public class GetEntityProperty : Activity {

        [XmlAttribute("Attribute")]
        public string Attribute;

        [XmlAttribute("Entity")]
        public string Entity;

        [XmlAttribute("EntityName")]
        public string EntityName;

        [XmlAttribute("Value")]
        public string Value;


        [XmlArray("GetEntityProperty.TargetType")]
        [XmlArrayItem("InArgument")]
        public Argument[] InArguments;

    }

    [XmlType("Postpone")]
    public class Postpone : Activity {
        
        [XmlAttribute("BlockExecution")]
        public string BlockExecution;

        [XmlAttribute("PostponeUntil")]
        public string PostponeUntil;
    }

    [XmlType("Null")]
    public class Null : Activity {

    }

    [XmlType("Assign")]
    public class Assign : Activity {

        [XmlAttribute("TypeArguments")]
        public string TypeArguments;

        [XmlAttribute("To")]
        public string To;

        [XmlAttribute("Value")]
        public string Value;
    }

    [XmlType("SendEmail")]
    public class SendEmail : Activity {

        [XmlAttribute("EntityId")]
        public string EntityId;

        [XmlAttribute("DisplayName")]
        public string DisplayName;

        [XmlAttribute("Entity")]
        public string Entity;
    }
}
