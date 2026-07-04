-- Minimal dev seed (customize as needed)
CREATE TABLE IF NOT EXISTS Plans(
  PlanId NVARCHAR(50) PRIMARY KEY,
  Name NVARCHAR(100),
  Tier NVARCHAR(50),
  Price DECIMAL(10,2),
  ValidityDays INT
);
INSERT INTO Plans(PlanId, Name, Tier, Price, ValidityDays) VALUES
 ('BASE499','Base 499','Base',499,30),
 ('SPORTS799','Sports 799','Premium',799,30);
