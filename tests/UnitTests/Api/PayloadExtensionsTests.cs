
using PostHog;
using PostHog.Api;

public class PayloadExtensionsTests
{
    public class TheAddToPayloadMethod
    {
        [Fact]
        public void AddsToGroups()
        {
            var groups = new GroupCollection();
            groups.Add("company", "acme");
            groups.Add("project", "123");
            var properties = new Dictionary<string, object>();

            groups.AddToPayload(properties);

            Assert.Equal(new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["company"] = "acme",
                    ["project"] = "123"
                }
            }, properties);
        }

        [Fact]
        public void AddsGroupsToGroups()
        {
            var groups = new GroupCollection
            {
                new Group("company", "acme"),
                new Group("project", "123")
            };
            var properties = new Dictionary<string, object>();

            groups.AddToPayload(properties);

            Assert.Equal(new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["company"] = "acme",
                    ["project"] = "123"
                }
            }, properties);
        }

        [Fact]
        public void AddsToGroupsWithCollectionInitializer()
        {
            var groups = new GroupCollection
            {
                { "company", "acme" },
                { "project", "42" }
            };
            var properties = new Dictionary<string, object>();

            groups.AddToPayload(properties);

            Assert.Equal(new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["company"] = "acme",
                    ["project"] = "42"
                }
            }, properties);
        }

        [Fact]
        public void AddsGroupPropertiesWithGroupInitializer()
        {
            var groups = new GroupCollection
            {
                new Group("project", "123")
                {
                    ["size"] = "large",
                    ["segment"] = "dinosaurs"
                },
                new Group("company", "acme")
                {
                    ["tier"] = "enterprise"
                }
            };
            var properties = new Dictionary<string, object>();

            groups.AddToPayload(properties);

            Assert.Equal(new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["company"] = "acme",
                    ["project"] = "123"
                },
                ["group_properties"] = new Dictionary<string, object>
                {
                    ["company"] = new Dictionary<string, object>
                    {
                        ["$group_key"] = "acme",
                        ["tier"] = "enterprise"
                    },
                    ["project"] = new Dictionary<string, object>
                    {
                        ["$group_key"] = "123",
                        ["size"] = "large",
                        ["segment"] = "dinosaurs"
                    }
                }
            }, properties);
        }

        [Fact]
        public void AddsToExistingGroupsAndGroupProperties()
        {
            var groups = new GroupCollection
            {
                new Group("project", "123")
                {
                    ["size"] = "large",
                    ["segment"] = "dinosaurs"
                },
                new Group("company", "acme")
                {
                    ["tier"] = "enterprise"
                }
            };
            var properties = new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["organization"] = "engineering"
                },
                ["group_properties"] = new Dictionary<string, Dictionary<string, object>>
                {
                    ["organization"] = new()
                    {
                        ["$group_key"] = "$organization",
                        ["department"] = "technology"
                    }
                }
            };

            groups.AddToPayload(properties);

            Assert.Equal(new Dictionary<string, object>
            {
                ["groups"] = new Dictionary<string, string>
                {
                    ["company"] = "acme",
                    ["project"] = "123",
                    ["organization"] = "engineering"
                },
                ["group_properties"] = new Dictionary<string, object>
                {
                    ["company"] = new Dictionary<string, object>
                    {
                        ["$group_key"] = "acme",
                        ["tier"] = "enterprise"
                    },
                    ["project"] = new Dictionary<string, object>
                    {
                        ["$group_key"] = "123",
                        ["size"] = "large",
                        ["segment"] = "dinosaurs"
                    },
                    ["organization"] = new Dictionary<string, object>
                    {
                        ["$group_key"] = "$organization",
                        ["department"] = "technology"
                    }
                }
            }, properties);
        }
    }
}