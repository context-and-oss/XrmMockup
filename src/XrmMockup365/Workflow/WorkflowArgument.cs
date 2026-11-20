using System.Runtime.Serialization;

namespace WorkflowExecuter
{
    [DataContract]
    public class WorkflowArgument
    {
        public enum DirectionType { Input, Output };
        [DataMember]
        public string Name { get; private set; }
        [DataMember]
        public bool Required { get; private set; }
        [DataMember]
        public bool IsTarget { get; private set; }
        [DataMember]
        public string Description { get; private set; }
        [DataMember]
        public DirectionType Direction { get; private set; }
        [DataMember]
        public string EntityLogicalName { get; private set; }

        public WorkflowArgument(string Name, bool Required, bool IsTarget, string Description, DirectionType Direction, string EntityLogicalName)
        {
            this.Name = Name;
            this.Required = Required;
            this.IsTarget = IsTarget;
            this.Description = Description;
            this.Direction = Direction;
            this.EntityLogicalName = EntityLogicalName;
        }

    }
}
