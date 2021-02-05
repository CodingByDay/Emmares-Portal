USE [Emmares4]
GO

/****** Object:  StoredProcedure [dbo].[Marketeer_GetChartStats]    Script Date: 13. 04. 2018 08:37:07 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[Marketeer_GetChartStats] 
	@chartType int,
	@user varchar(48)
AS
BEGIN
	SET NOCOUNT ON;

	declare @myAverage float;
	declare @communityAverage float;
	declare @totalRatings int;

	IF @chartType = 0 --daily
	BEGIN		
		SELECT cast(coalesce(AVG(CAST(s.Rating as float)), 0.0) as numeric(10,2)) As AverageScore, count(*) As RatedEmails, count(distinct s.UserId) As Evaluators
		FROM [Statistics] s
		LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
		LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
		WHERE p.OwnerId = @user
		AND cast(s.DateAdded as date) = cast(GETDATE() as date)
	END

	IF @chartType = 1 --Week 
	BEGIN
		SELECT cast(coalesce(AVG(CAST(s.Rating as float)), 0.0) as numeric(10,2)) As AverageScore, count(*) As RatedEmails, count(distinct s.UserId) As Evaluators
		FROM [Statistics] s
		LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
		LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
		WHERE p.OwnerId = @user
		AND (s.DateAdded between CAST(DATEADD(day, 1-DATEPART(weekday, GETDATE()), GETDATE()) AS date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
	END

	IF @chartType = 2 --Monthly 
	BEGIN	
		SELECT cast(coalesce(AVG(CAST(s.Rating as float)), 0.0) as numeric(10,2)) As AverageScore, count(*) As RatedEmails, count(distinct s.UserId) As Evaluators
		FROM [Statistics] s
		LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
		LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
		WHERE p.OwnerId = @user
		AND (s.DateAdded between DATEADD(day, -(datepart(day,getdate())) +1 , getdate()) and CONVERT(date, DateAdd(day,1,GETDATE())))
	END

	IF @chartType = 3 --Year 
	BEGIN
		SELECT cast(coalesce(AVG(CAST(s.Rating as float)), 0.0) as numeric(10,2)) As AverageScore, count(*) As RatedEmails, count(distinct s.UserId) As Evaluators
		FROM [Statistics] s
		LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
		LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
		WHERE p.OwnerId = @user
		AND (s.DateAdded between cast(DATEADD(yy, DATEDIFF(yy, 0, GETDATE()) - 1, 0) as date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
	END
END
GO


