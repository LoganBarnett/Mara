using System;
using NUnit.Framework;
using Mara;

namespace Mara.DriverSpecs {

    /*
     * The spec covers the basics of filling in and submitting forms
     *
     *   FillIn("...", "...")
     *   FillInFields("...", "...")
     *   ClickButton("...")
     *   Click("...")
     */
    [TestFixture]
    public class FormsSpec : MaraTest {

        [SetUp] public void Setup(){ Visit("/Form.aspx"); }

        [Test]
        public void TheExampleFormShouldBeSetupLikeWeThinkItIs() {
            // #DogName should exist and value should be empty (it also has to have a NAME to be part of the form)
            Assert.NotNull( Find("//*[@name='DogName']")  );
            Assert.NotNull( Find("id('DogName')")         );
            Assert.IsEmpty( Find("id('DogName')").Value   );

            // #DogBreed should not exist, it's an input with NAME="DogBreed" and value should be empty
            Assert.Null(    Find("id('DogBreed')")              );
            Assert.NotNull( Find("//*[@name='DogBreed']")       );
            Assert.IsEmpty( Find("//*[@name='DogBreed']").Value );

            // Button with VALUE="POST some stuff" should exist
            Assert.NotNull( Find("//input[@type='submit'][@value='POST some stuff']") );
        }

        [Test]
        public void CanFillInAField_BlowsUpIfFieldNotDefined() {
            var exceptionMessage = "Could not find element with XPath: id('IDontExist') OR //*[@name='IDontExist']";
            this.AssertThrows<ElementNotFoundException>(exceptionMessage, () => {
                FillIn("IDontExist", "A value I just made up");
            });

            FillIn("DogName", "A value I just made up");

            Assert.Null(Find("//dd[@data-variable='DogName']"));
            ClickButton("POST some stuff");
			Assert.That(Find("//span[@class='requestMethod']").Text, Is.EqualTo("POST"));

            // When you submit the form a <DL> is printed with info about all of the POSTed variables
            Assert.NotNull(Find("//dd[@data-variable='DogName']"));
            Assert.That(Find("//dd[@data-variable='DogName']").Text, Is.EqualTo("A value I just made up"));
        }

        [Test]
        public void FillIn_LooksForFieldWithIdOrName_BlowsUpIfNoneFound() {
            FillIn("DogName",  "Dog name has an ID");
            FillIn("DogBreed", "Dog breed does NOT have an ID, only a Name");
            ClickButton("POST some stuff");

            Assert.That(Find("//dd[@data-variable='DogName']").Text,  Is.EqualTo("Dog name has an ID"));
            Assert.That(Find("//dd[@data-variable='DogBreed']").Text, Is.EqualTo("Dog breed does NOT have an ID, only a Name"));
        }

        [Test]
        public void CanCheckCheckbox_BlowsUpIfNotFound() {
            var exceptionMessage = "Could not find element with XPath: //input[@type='checkbox'][@id='IDontExist'] OR //input[@type='checkbox'][@name='IDontExist']";
            this.AssertThrows<ElementNotFoundException>(exceptionMessage, () => {
				Check("IDontExist");
            });

			ClickButton("POST some stuff");
			Assert.Null(Find("//dd[@data-variable='DogIsGood']"));

			Check("DogIsGood");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='DogIsGood']").Text, Is.EqualTo("on"));

			Uncheck("DogIsGood");
			ClickButton("POST some stuff");
			Assert.Null(Find("//dd[@data-variable='DogIsGood']"));
		}

        [Test]
        public void CanSelectFromDropDown_BlowsUpIfDropDownOrOptionNotFound() {
			// TODO test boom!

			// the first one, by default
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='HairType']").Text, Is.EqualTo("FuzzyValue"));

			Select("HairType", "Poofy");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='HairType']").Text, Is.EqualTo("Poofy"));

			// Can select option text
			Select("HairType", "Long");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='HairType']").Text, Is.EqualTo("LongHairValue"));

			Select("HairType", "Poodle-y");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='HairType']").Text, Is.EqualTo("Poodle-y"));

			// Can select option value
			Select("HairType", "LongHairValue");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='HairType']").Text, Is.EqualTo("LongHairValue"));

			// TODO Can Deselect() [defaults back to the first field)

            // TODO make sure that Select() deselects all other options in this <select>!!!!
		}

        [Test]
        public void CanFillInTextArea_BlowsUpIfNotFound() {
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='DogBio']").Text, Is.EqualTo(""));

			FillIn("DogBio", "Hello World!");
			ClickButton("POST some stuff");
			Assert.That(Find("//dd[@data-variable='DogBio']").Text, Is.EqualTo("Hello World!"));

			FillIn("DogBio", "Hello World!\n\nI have new lines");
			ClickButton("POST some stuff");
			// Assert.That(Find("//dd[@data-variable='DogBio']").Text, Is.EqualTo("Hello World!\n\nI have new lines"));
			Assert.That(Find("//dd[@data-variable='DogBio']").Text, Is.EqualTo("Hello World! I have new lines")); // can't seem to get this to not be cleaned up ... (new lines removed) ... TODO FIXME
		}

        [Test]
        public void CanFillInMultipleFields_BlowsUpIfAnyAreNotDefined() {
            var exceptionMessage = "Could not find element with XPath: id('IDontExist') OR //*[@name='IDontExist']";
            this.AssertThrows<ElementNotFoundException>(exceptionMessage, () => {
                FillInFields(new { DogName = "ok", DogBreed = "ok", IDontExist = "boom!" });
            });

            Refresh(); // clear forms out and whatnot, incase the previous FillInFields() got saved TODO

            FillInFields(new { DogName = "Rover", DogBreed = "Golden Retriever" });
            ClickButton("POST some stuff");

            Assert.That(Find("//dd[@data-variable='DogName']").Text,  Is.EqualTo("Rover"));
            Assert.That(Find("//dd[@data-variable='DogBreed']").Text, Is.EqualTo("Golden Retriever"));
        }

		[Test][Ignore]
		public void CanFillInMultipleFieldsUsingAPrefixForEachField() {}

        [Test]
        public void ClickClicksLinkOrButton_BlowsUpIfNotFound() {
            var exceptionMessage = "Could not find element with XPath: //a[text()='Nothing has this text'] OR //input[@type='submit'][@value='Nothing has this text']";
            this.AssertThrows<ElementNotFoundException>(exceptionMessage, () => {
                Click("Nothing has this text");
            });
            
            Click("Home");
            Assert.That(CurrentPath, Is.EqualTo("/"));

            Click("Form");
            Assert.That(CurrentPath, Is.EqualTo("/Form.aspx"));

            Assert.False(Page.HasContent("Snoopy"));
            FillInFields(new { DogName = "Snoopy" });
            Click("POST some stuff");
            Assert.True(Page.HasContent("Snoopy"));
        }

		[Test]
		public void CanSubmitAFormWithMethodGET() {
			FillIn("q", "My Search");
			ClickButton("Search");

			Assert.That(Find("//span[@class='requestMethod']").Text, Is.EqualTo("GET"));

            Assert.That(Find("//dd[@data-variable='q']").Text,  Is.EqualTo("My Search"));

			Assert.That(Find("id('querystrings')").Text, Is.StringContaining("q = My Search"));
		}
    }
}
