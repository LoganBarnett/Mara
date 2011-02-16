<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Title="Untitled Page"  %>
<asp:Content ID="Content" Runat="Server" ContentPlaceHolderID="Container" >

<h3>Request method: <span class="requestMethod"><%= Request.HttpMethod %></span></h3>

<% var postAndQueryStringVariables = new NameValueCollection(Request.Form); postAndQueryStringVariables.Add(Request.QueryString); %>
<% if (postAndQueryStringVariables.Count > 0) { %>
<h2>Params</h2>
<dl id="variables">
  <% foreach (var variable in postAndQueryStringVariables.AllKeys) { %>
  <dt data-variable="<%= variable %>"><%= variable %>:</dt>
  <dd data-variable="<%= variable %>"><%= postAndQueryStringVariables[variable] %></dd>
  <% } %>
</dl>
<div style="clear: left;" />
<% } %>

<h1>Form</h1>
<form action="/Form.aspx" method="post">
  <fieldset>
    <legend>Fill out some stuff to POST</legend>
      <label for="DogName">Dog name</label>
      <input type="text" id="DogName" name="DogName" placeholder="Enter dog name here" value="<%= Request["DogName"] %>" autofocus="true" />

      <!-- DogBreed doesn't have an ID attribute on purpose ... don't add it. -->
      <label for="DogBreed">Breed of dog</label>
      <input type="text" name="DogBreed" placeholder="eg. Golden Retriever" value="<%= Request["DogBreed"] %>" />

	  <label for="HairType">Hair Type</label>
	  <select id="HairType" name="HairType">
		<option value="FuzzyValue">Fuzzy</option>
		<option>Poofy</option>
		<option>Poodle-y</option>
		<option value="LongHairValue">Long</option>
	  </select>

      <label for="DogBio">Dog Bio:</label>
      <textarea id="DogBio" name="DogBio"></textarea>

	  <p><input type="checkbox" id="DogIsGood" name="DogIsGood" /><label for="DogIsGood">Dog is good?</label></p>

      <input type="submit" value="POST some stuff" />
  </fieldset>
</form>

<h2>GET Form</h2>
<pre id="querystrings">
<% for (int i = 0; i < Request.QueryString.Count; i++) { %>
<%= Request.QueryString.AllKeys[i] %> = <%= Request.QueryString[i] %>
<% } %>
</pre>
<form action="/Form.aspx" method="get">
    <input name="q" type="text" placeholder="Search query ..." />
    <input type="submit" value="Search" />
</form>

<input type="text" name="OutsideOfForm" value="I am outside of the form!" />
</asp:Content>
