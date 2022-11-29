create table custom_mobile 
(
	id int auto_increment primary key,
    mobile_no varchar(100),
    status tinyint default 1, 
    created_at datetime default now(),
	updated_at datetime 
)