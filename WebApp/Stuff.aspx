<%@ Page Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" Title="Untitled Page"  %>
<asp:Content ID="Content" Runat="Server" ContentPlaceHolderID="Container" >
<h1>Miscellaneous Stuff</h1>
<input type="text" name="DogName" value="Rover" />
<pre>Dogs have names</pre>
<p>
  Hi, how goes?
  <br/>
  Does it go well?
</p>
<div id="i_haz_pre">
  <pre>
I
am
another
pre
  </pre>
</div>
<p>HostName: <%= Request.Url.Host %></p>
</asp:Content>
