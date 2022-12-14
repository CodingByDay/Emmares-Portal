USE [Emmares4]
GO
/****** Object:  StoredProcedure [dbo].[GetChartCampaignsHitsStats]    Script Date: 10/08/2018 14:26:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetChartCampaignsHitsStats] 
	@chartType int,
	@user varchar(48),
	@campID uniqueidentifier = NULL
AS
BEGIN
	SET NOCOUNT ON;

	declare @myAverage float;
	declare @communityAverage float;
	declare @totalRatings int;

	IF @chartType = 0 --daily
	BEGIN
		IF @campID IS NOT NULL
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) as TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND ch.CampaignID = @campID
			AND cast(ch.Time as date) = cast(GETDATE() as date)
		ELSE
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) as TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND cast(ch.Time as date) = cast(GETDATE() as date)
	END

	IF @chartType = 1 --Week 
	BEGIN
		IF @campID IS NOT NULL
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND ch.CampaignID = @campID
			AND (ch.Time between CAST(DATEADD(day, 1-DATEPART(weekday, GETDATE()), GETDATE()) AS date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
		ELSE
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND (ch.Time between CAST(DATEADD(day, 1-DATEPART(weekday, GETDATE()), GETDATE()) AS date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
	END

	IF @chartType = 2 --Monthly 
	BEGIN
		IF @campID IS NOT NULL
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND ch.CampaignID = @campID
			AND (ch.Time between DATEADD(day, -(datepart(day,getdate())) +1 , getdate()) and CONVERT(date, DateAdd(day,1,GETDATE())))
		ELSE
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND (ch.Time between DATEADD(day, -(datepart(day,getdate())) +1 , getdate()) and CONVERT(date, DateAdd(day,1,GETDATE())))
	END

	IF @chartType = 3 --Year 
	BEGIN
		IF @campID IS NOT NULL
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND ch.CampaignID = @campID
			AND (ch.Time between cast(DATEADD(yy, DATEDIFF(yy, 0, GETDATE()) - 1, 0) as date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
		ELSE
			SELECT cast(coalesce(COUNT(ch.CampaignID), 0.0) as numeric(10,2)) As TotalHits
			FROM CampaignsHits ch
			LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user
			AND (ch.Time between cast(DATEADD(yy, DATEDIFF(yy, 0, GETDATE()) - 1, 0) as date)  and CONVERT(date, DateAdd(day,1,GETDATE())))
	END
END
