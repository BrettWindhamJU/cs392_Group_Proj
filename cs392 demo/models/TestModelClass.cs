using System.ComponentModel.DataAnnotations;

namespace cs392_demo.models
{
    public class TestModelClass
    {
        public string testVal { get; set; }

        [Key] public int testKey {  get; set; }
    }
}
