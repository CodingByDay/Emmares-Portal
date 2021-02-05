USE [Emmares4]
GO

/****** Object:  StoredProcedure [dbo].[GetChartStats]    Script Date: 13. 04. 2018 08:36:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[GetChartStats] 
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
		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #mya_statsd
		FROM [Statistics] s
		WHERE s.UserId = @user
		AND cast(s.DateAdded as date) = cast(GETDATE() as date);

		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #coma_statsd
		FROM [Statistics] s
		WHERE s.UserId != @user
		AND cast(s.DateAdded as date) = cast(GETDATE() as date);

		select @myAverage = AverageRating, @totalRatings = statCount
		from #mya_statsd;

		select @communityAverage = AverageRating
		from #coma_statsd;

		select @totalRatings as RatedEmails, cast(@myAverage  as numeric(10,2)) As AverageScore,
			  cast(case 
				when @totalRatings = 0 then 0  
				else
					case
						when @communityAverage = 0 then 100 		
						else (@myAverage/@communityAverage) * 100 
					end
			end as decimal(10,2)) as CommunityMatch;

		drop table #mya_statsd;
		drop table #coma_statsd;
	END

	IF @chartType = 1 --Week 
	BEGIN
		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #mya_statsw
		FROM [Statistics] s
		WHERE s.UserId = @user
		AND (s.DateAdded between CAST(DATEADD(day, 1-DATEPART(weekday, GETDATE()), GETDATE()) AS date)  and CONVERT(date, DateAdd(day,1,GETDATE())))

		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #coma_statsw
		FROM [Statistics] s
		WHERE s.UserId != @user
		AND (s.DateAdded between CAST(DATEADD(day, 1-DATEPART(weekday, GETDATE()), GETDATE()) AS date)  and CONVERT(date, DateAdd(day,1,GETDATE())))


		select @myAverage = AverageRating, @totalRatings = statCount
		from #mya_statsw

		select @communityAverage = AverageRating
		from #coma_statsw

		select @totalRatings as RatedEmails, cast(@myAverage  as numeric(10,2)) As AverageScore,
		cast(case 
		when @totalRatings = 0 then 0  
		else
		case
			when @communityAverage = 0 then 100 		
			else (@myAverage/@communityAverage) * 100 
		end
		end as numeric(10,2)) as CommunityMatch
	
		drop table #mya_statsw;
		drop table #coma_statsw;
	END

	IF @chartType = 2 --Monthly 
	BEGIN	
		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #mya_statsm
		FROM [Statistics] s
		WHERE s.UserId = @user
		AND (s.DateAdded between DATEADD(day, -(datepart(day,getdate())) +1 , getdate()) and CONVERT(date, DateAdd(day,1,GETDATE())))

		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #coma_statsm
		FROM [Statistics] s
		WHERE s.UserId != @user
		AND (s.DateAdded between DATEADD(day, -(datepart(day,getdate())) +1 , getdate()) and CONVERT(date, DateAdd(day,1,GETDATE())))

		select @myAverage = AverageRating, @totalRatings = statCount
		from #mya_statsm

		select @communityAverage = AverageRating
		from #coma_statsm

		select @totalRatings as RatedEmails, cast(@myAverage as numeric(10,2)) As AverageScore, 
				cast( case 
						when @totalRatings = 0 then 0  
						else
							case
								when @communityAverage = 0 then 100 		
								else (@myAverage/@communityAverage) * 100 
							end
					end as numeric(10,2))  as CommunityMatch

		drop table #mya_statsm;
		drop table #coma_statsm;
	END

	IF @chartType = 3 --Year 
	BEGIN
		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #mya_statsy
		FROM [Statistics] s
		WHERE s.UserId = @user
		AND (s.DateAdded between cast(DATEADD(yy, DATEDIFF(yy, 0, GETDATE()) - 1, 0) as date)  and CONVERT(date, DateAdd(day,1,GETDATE())))

		SELECT coalesce(AVG(CAST(s.Rating as float)), 0.0) As AverageRating, count(*) statCount into #coma_statsy
		FROM [Statistics] s
		WHERE s.UserId != @user
		AND (s.DateAdded between cast(DATEADD(yy, DATEDIFF(yy, 0, GETDATE()) - 1, 0) as date)  and CONVERT(date, DateAdd(day,1,GETDATE())))

		select @myAverage = AverageRating, @totalRatings = statCount
		from #mya_statsy

		select @communityAverage = AverageRating
		from #coma_statsy

		select @totalRatings as RatedEmails, cast(@myAverage as numeric(10,2)) As AverageScore,
		cast(case 
		when @totalRatings = 0 then 0  
		else
			case
				when @communityAverage = 0 then 100 		
				else (@myAverage/@communityAverage) * 100 
			end
		end as numeric(10,2)) as CommunityMatch

		drop table #mya_statsy;
		drop table #coma_statsy;
	END

END
GO


