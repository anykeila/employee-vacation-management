using Xunit;

namespace VacationManagement.IntegrationTests;

// One PostgreSQL container is shared by every test in the collection: the container
// (and its migrated schema) is expensive to spin up, and the tests use disjoint date
// windows so the company-wide overlap rule does not bleed across cases.
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>;
