USE [Emmares4]
GO
/****** Object:  StoredProcedure [dbo].[GetChartCampaignsHitsData]    Script Date: 10/08/2018 13:53:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[GetChartCampaignsHitsData] 
	@chartType int,
	@user varchar(48),
	@campID uniqueidentifier = NULL
AS
BEGIN
	SET NOCOUNT ON;

	declare @dt date = cast(GetDate() as date);
	declare @dtstart date =  DATEADD(day, -DATEPART(day, @dt) + 1, @dt);
	declare @dtend date = dateadd(DAY, -1, DATEADD(MONTH, 1, @dtstart));
	declare @numberOfWeeks int;

	declare @myAverage float;
	declare @communityAverage float;
	declare @totalRatings int;

	IF @chartType = 0 --daily
	BEGIN
		IF @campID IS NOT NULL
			select FORMAT(spt.n, '00') as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT DISTINCT n = number 
			FROM master..[spt_values]
			WHERE number BETWEEN 0 AND 24) spt
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'HH') as Label, cast(count(ch.CampaignID) as float) as Hits
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and ch.CampaignID = @campID
				and cast(ch.Time as date) = cast(GETDATE() as date)
				GROUP BY FORMAT(ch.Time, 'HH')
			) chart ON FORMAT(spt.n, '00') = chart.Label
		ELSE
			select FORMAT(spt.n, '00') as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT DISTINCT n = number 
			FROM master..[spt_values]
			WHERE number BETWEEN 0 AND 24) spt
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'HH') as Label, cast(count(ch.CampaignID) as float) as Hits
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and cast(ch.Time as date) = cast(GETDATE() as date)
				GROUP BY FORMAT(ch.Time, 'HH')
			) chart ON FORMAT(spt.n, '00') = chart.Label
	END

	IF @chartType = 1 --Week 
	BEGIN
		IF @campID IS NOT NULL
			select d as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT d 
			FROM (values('Sat'),('Sun'),('Mon'),('Tue'),('Wed'),('Thu'),('Fri')) weekdays(d)) ds
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'ddd') as Label, cast(COUNT(ch.CampaignID) as float) as Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and ch.CampaignID = @campID
				and  DATEPART(WEEK, ch.Time) = DATEPART(WEEK, GETDATE())
				GROUP BY FORMAT(ch.Time, 'ddd')
			) chart ON ds.d = chart.Label;
		ELSE
			select d as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT d 
			FROM (values('Sat'),('Sun'),('Mon'),('Tue'),('Wed'),('Thu'),('Fri')) weekdays(d)) ds
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'ddd') as Label, cast(COUNT(ch.CampaignID) as float) as Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and  DATEPART(WEEK, ch.Time) = DATEPART(WEEK, GETDATE())
				GROUP BY FORMAT(ch.Time, 'ddd')
			) chart ON ds.d = chart.Label;
	END

	IF @chartType = 2 --Monthly 
	BEGIN	

		WITH dates AS (
			 SELECT @dtstart ADate
			 UNION ALL
			 SELECT DATEADD(day, 1, t.ADate) 
			   FROM dates t
			  WHERE DATEADD(day, 1, t.ADate) <= @dtend
		)
		SELECT top 1 DatePart(WEEKDAY, ADate) weekday, COUNT(*) weeks into #ss
		  FROM dates d
		  group by DatePart(WEEKDAY, ADate)
		  order by 2 desc;

		select @numberOfWeeks = weeks from #ss;
		drop table #ss;

		IF @campID IS NOT NULL
			select 'Week ' + cast(spt.n as varchar(2)) as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT DISTINCT n = number 
			FROM master..[spt_values]
			WHERE number BETWEEN 1 AND @numberOfWeeks) spt
			left outer join 
			(
				SELECT (DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, ch.Time), 0), ch.Time) + 1) as Label,  cast(COUNT(ch.CampaignID) as float) as Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and ch.CampaignID = @campID
				and DATEPART(MONTH, ch.Time) = DATEPART(MONTH, GETDATE())
				GROUP BY DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, ch.Time), 0), ch.Time) +1
			) chart ON spt.n = chart.Label;
		ELSE
			select 'Week ' + cast(spt.n as varchar(2)) as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT DISTINCT n = number 
			FROM master..[spt_values]
			WHERE number BETWEEN 1 AND @numberOfWeeks) spt
			left outer join 
			(
				SELECT (DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, ch.Time), 0), ch.Time) + 1) as Label,  cast(COUNT(ch.CampaignID) as float) as Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and DATEPART(MONTH, ch.Time) = DATEPART(MONTH, GETDATE())
				GROUP BY DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, ch.Time), 0), ch.Time) +1
			) chart ON spt.n = chart.Label;
	END

	IF @chartType = 3 --Year 
	BEGIN
		IF @campID IS NOT NULL
			select d as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT d 
			FROM (values('Jan'),('Feb'),('Mar'),('Apr'),('May'),('Jun'),('Jul'),('Aug'),('Sep'),('Oct'),('Nov'),('Dec')) mons(d)) mn
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'MMM') as Label, COUNT(ch.CampaignID) As Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and ch.CampaignID = @campID
				and DATEPART(YEAR, ch.Time) = DATEPART(YEAR, GETDATE())
				GROUP BY FORMAT(ch.Time, 'MMM')
			) chart ON mn.d = chart.Label
		ELSE
			select d as Label, coalesce(chart.Hits, 0.0) as Ratings from
			(SELECT d 
			FROM (values('Jan'),('Feb'),('Mar'),('Apr'),('May'),('Jun'),('Jul'),('Aug'),('Sep'),('Oct'),('Nov'),('Dec')) mons(d)) mn
			left outer join 
			(
				SELECT FORMAT(ch.Time, 'MMM') as Label, COUNT(ch.CampaignID) As Hits 
				FROM CampaignsHits ch
				LEFT OUTER JOIN Campaigns cmp on ch.CampaignID = cmp.ID
				LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
				WHERE p.OwnerId = @user
				and DATEPART(YEAR, ch.Time) = DATEPART(YEAR, GETDATE())
				GROUP BY FORMAT(ch.Time, 'MMM')
			) chart ON mn.d = chart.Label
	END
END
