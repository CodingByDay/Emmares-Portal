USE [Emmares4]
GO

/****** Object:  StoredProcedure [dbo].[Marketeer_GetChartData]    Script Date: 13. 04. 2018 08:36:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO





CREATE PROCEDURE [dbo].[Marketeer_GetChartData] 
	@chartType int,
	@user varchar(48)
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
		select FORMAT(spt.n, '00') as Label, coalesce(chart.AvarageRating, 0.0) as Ratings from
		(SELECT DISTINCT n = number 
		FROM master..[spt_values]
		WHERE number BETWEEN 0 AND 24) spt
		left outer join 
		(
			SELECT FORMAT(s.DateAdded, 'HH') as Label, coalesce(AVG(CAST(s.Rating as float)), 0.0) As AvarageRating 
			FROM [Statistics] s
			LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user and cast(s.DateAdded as date) = cast(GETDATE() as date)
			GROUP BY FORMAT(s.DateAdded, 'HH')
		) chart ON FORMAT(spt.n, '00') = chart.Label	
	END

	IF @chartType = 1 --Week 
	BEGIN
		select d as Label, coalesce(chart.AvarageRating, 0.0) as Ratings from
		(SELECT d 
		FROM (values('Sat'),('Sun'),('Mon'),('Tue'),('Wed'),('Thu'),('Fri')) weekdays(d)) ds
		left outer join 
		(
			SELECT FORMAT(s.DateAdded, 'ddd') as Label, coalesce(AVG(CAST(s.Rating as float)), 0.0) As AvarageRating 
			FROM [Statistics] s
			LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user and  DATEPART(WEEK, s.DateAdded) = DATEPART(WEEK, GETDATE())
			GROUP BY FORMAT(s.DateAdded, 'ddd')
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

		select 'Week ' + cast(spt.n as varchar(2)) as Label, coalesce(chart.AvarageRating, 0.0) as Ratings from
		(SELECT DISTINCT n = number 
		FROM master..[spt_values]
		WHERE number BETWEEN 1 AND @numberOfWeeks) spt
		left outer join 
		(
			SELECT (DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, s.DateAdded), 0), s.DateAdded) + 1) as Label,  coalesce(AVG(CAST(s.Rating as float)), 0.0) As AvarageRating 
			FROM [Statistics] s
			LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user  and DATEPART(MONTH, s.DateAdded) = DATEPART(MONTH, GETDATE())
			GROUP BY DATEDIFF(WEEK, DATEADD(MONTH, DATEDIFF(MONTH, 0, s.DateAdded), 0), s.DateAdded) +1
		) chart ON spt.n = chart.Label;
	END

	IF @chartType = 3 --Year 
	BEGIN
		select d as Label, coalesce(chart.AvarageRating, 0.0) as Ratings from
		(SELECT d 
		FROM (values('Jan'),('Feb'),('Mar'),('Apr'),('May'),('Jun'),('Jul'),('Aug'),('Sep'),('Oct'),('Nov'),('Dec')) mons(d)) mn
		left outer join 
		(
			SELECT FORMAT(s.DateAdded, 'MMM') as Label,  coalesce(AVG(CAST(s.Rating as float)), 0.0) As AvarageRating 
			FROM [Statistics] s
			LEFT OUTER JOIN Campaigns cmp on s.CampaignID = cmp.ID
			LEFT OUTER JOIN Providers p on p.ID = cmp.PublisherID
			WHERE p.OwnerId = @user and DATEPART(YEAR, s.DateAdded) = DATEPART(YEAR, GETDATE())
			GROUP BY FORMAT(s.DateAdded, 'MMM')
		) chart ON mn.d = chart.Label
	END

END
GO


