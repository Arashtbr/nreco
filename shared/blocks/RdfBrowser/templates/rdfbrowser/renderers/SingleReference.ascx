﻿<%@ Control Language="c#" AutoEventWireup="false" EnableViewState="false" CodeFile="PropertyValueRenderer.ascx.cs" Inherits="PropertyValueRenderer" TargetSchema="http://schemas.microsoft.com/intellisense/ie5" %>
<a href="rdfbrowser.aspx?resource=<%# HttpUtility.UrlEncode( Property.Reference.Uid.Uri ) %>"><%# Property.Reference.Label %></a>