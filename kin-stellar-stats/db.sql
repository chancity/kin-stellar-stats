-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.7.20-log - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             9.4.0.5125
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for kin_test
CREATE DATABASE IF NOT EXISTS `kin_test` /*!40100 DEFAULT CHARACTER SET utf8 */;
USE `kin_test`;

-- Dumping structure for table kin_test.flattenedbalance
CREATE TABLE IF NOT EXISTS `flattenedbalance` (
  `KinAccountId` varchar(255) NOT NULL,
  `AssetCode` longtext,
  `AssetType` varchar(255) NOT NULL,
  `BalanceString` longtext,
  `Limit` longtext,
  PRIMARY KEY (`KinAccountId`,`AssetType`),
  CONSTRAINT `FK_FlattenedBalance_KinAccounts_KinAccountId` FOREIGN KEY (`KinAccountId`) REFERENCES `kinaccounts` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table kin_test.flattenedbalance: ~0 rows (approximately)
/*!40000 ALTER TABLE `flattenedbalance` DISABLE KEYS */;
/*!40000 ALTER TABLE `flattenedbalance` ENABLE KEYS */;

-- Dumping structure for table kin_test.flattenedoperation
CREATE TABLE IF NOT EXISTS `flattenedoperation` (
  `Id` bigint(20) NOT NULL AUTO_INCREMENT,
  `CreatedAt` datetime(6) NOT NULL,
  `PagingToken` longtext,
  `SourceAccount` longtext,
  `TransactionHash` longtext,
  `Type` longtext,
  `EffectType` longtext,
  `Memo` longtext,
  `Discriminator` longtext NOT NULL,
  `Account` longtext,
  `Funder` longtext,
  `StartingBalance` longtext,
  `Amount` longtext,
  `AssetCode` longtext,
  `AssetIssuer` longtext,
  `AssetType` longtext,
  `From` longtext,
  `To` longtext,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table kin_test.flattenedoperation: ~0 rows (approximately)
/*!40000 ALTER TABLE `flattenedoperation` DISABLE KEYS */;
/*!40000 ALTER TABLE `flattenedoperation` ENABLE KEYS */;

-- Dumping structure for table kin_test.kinaccounts
CREATE TABLE IF NOT EXISTS `kinaccounts` (
  `Id` varchar(255) NOT NULL,
  `Memo` longtext,
  `AccountCreditedCount` int(11) NOT NULL,
  `AccountDebitedCount` int(11) NOT NULL,
  `AccountCreditedVolume` int(11) NOT NULL,
  `AccountDebitedVolume` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `LastActive` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table kin_test.kinaccounts: ~911 rows (approximately)
/*!40000 ALTER TABLE `kinaccounts` DISABLE KEYS */;
/*!40000 ALTER TABLE `kinaccounts` ENABLE KEYS */;

-- Dumping structure for table kin_test.paginations
CREATE TABLE IF NOT EXISTS `paginations` (
  `CursorType` varchar(255) NOT NULL,
  `PagingToken` longtext,
  PRIMARY KEY (`CursorType`),
  UNIQUE KEY `IX_Paginations_CursorType` (`CursorType`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table kin_test.paginations: ~0 rows (approximately)
/*!40000 ALTER TABLE `paginations` DISABLE KEYS */;
/*!40000 ALTER TABLE `paginations` ENABLE KEYS */;

-- Dumping structure for table kin_test.__efmigrationshistory
CREATE TABLE IF NOT EXISTS `__efmigrationshistory` (
  `MigrationId` varchar(95) NOT NULL,
  `ProductVersion` varchar(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

-- Dumping data for table kin_test.__efmigrationshistory: ~3 rows (approximately)
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
	('20180725013825_Migration1', '2.1.1-rtm-30846');
INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
	('20180725020854_Migration2', '2.1.1-rtm-30846');
INSERT INTO `__efmigrationshistory` (`MigrationId`, `ProductVersion`) VALUES
	('20180725021104_Migration3', '2.1.1-rtm-30846');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
