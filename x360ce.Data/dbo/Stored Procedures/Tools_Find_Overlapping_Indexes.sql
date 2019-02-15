
CREATE PROCEDURE [dbo].[Tools_Find_Overlapping_Indexes]
	
AS
-- http://sqlblog.com/blogs/paul_nielsen/archive/2008/06/25/find-duplicate-indexes.aspx

-- EXEC [dbo].[Tools_Find_Overlapping_Indexes]

-- Overlapping indexes
with indexcols as
(
select object_id as id, index_id as indid, name,
(select case keyno when 0 then NULL else colid end as [data()]
from sys.sysindexkeys as k
where k.id = i.object_id
and k.indid = i.index_id
order by keyno, colid
for xml path('')) as cols
from sys.indexes as i
)
select
object_schema_name(c1.id) + '.' + object_name(c1.id) as 'table',
c1.name as 'index',
c2.name as 'partialduplicate'
from indexcols as c1
join indexcols as c2
on c1.id = c2.id
and c1.indid < c2.indid
and (c1.cols like c2.cols + '%' 
or c2.cols like c1.cols + '%') ;