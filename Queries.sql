DELETE FROM transactions."MessageProcessingRecords";
DELETE FROM transactions."BankTransactions";
DELETE FROM banks."InboundWebhookMessages";
DELETE FROM banks."bank_transaction_stage";

SELECT 
(SELECT COUNT(1) FROM transactions."MessageProcessingRecords") as "Messages Processed Count",
(SELECT COUNT(1) FROM transactions."BankTransactions") as "Transactions",
(SELECT COUNT(1) FROM transactions."bank_transaction_stage") as "Staging Transactions",
(SELECT COUNT(1) FROM banks."InboundWebhookMessages") as "Messages";


SELECT COUNT(1)