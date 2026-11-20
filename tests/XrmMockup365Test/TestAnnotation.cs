using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DG.XrmFramework.BusinessDomain.ServiceContext;
using Xunit;

namespace DG.XrmMockupTest
{
    public class TestAnnotation : UnitTestBase
    {
        public TestAnnotation(XrmMockupFixture fixture) : base(fixture) { }

        [Fact]
        public void TestAnnotationsCreatedWithAttachmentsHaveIsDocumentSet()
        {
            var note = new Entity("annotation");
            note["documentbody"] = "base64string";
            var noteId = orgAdminService.Create(note);

            var checkNote = orgAdminService.Retrieve("annotation", noteId, new ColumnSet(true));
            Assert.True(checkNote.GetAttributeValue<bool>("isdocument"));
        }

        [Fact]
        public void TestAnnotationsCreatedWithAttachmentsRemovedHaveIsDocumentNotSet()
        {
            var note = new Entity("annotation");
            note["documentbody"] = "base64string";
            var noteId = orgAdminService.Create(note);

            note.Id = noteId;
            note["documentbody"] = "";
            orgAdminService.Update(note);

            var checkNote = orgAdminService.Retrieve("annotation", noteId, new ColumnSet(true));
            Assert.False(checkNote.GetAttributeValue<bool>("isdocument"));
        }

        [Fact]
        public void TestAnnotationsUpdatedWithAttachmentsHaveIsDocumentSet()
        {
            var note = new Entity("annotation");
            var noteId = orgAdminService.Create(note);

            note.Id = noteId;
            note["documentbody"] = "base64string";
            orgAdminService.Update(note);

            var checkNote = orgAdminService.Retrieve("annotation", noteId, new ColumnSet(true));
            Assert.True(checkNote.GetAttributeValue<bool>("isdocument"));
        }
    }
}
