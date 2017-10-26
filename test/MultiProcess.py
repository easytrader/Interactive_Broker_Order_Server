import multiprocessing
import requests

API = {
	"stk_long":{
		"function":"placeOrder",
		"password":"blahblahblah",
		"strategyName":"strategy1",
		"strategyOrderNumber":1,
		"instrumentType":"STK",
		"symbol":"AAPL",
		"currency":"USD",
		"action":"Long",
		"quantity":10
	},
	"stk_short":{
		"function":"placeOrder",
		"password":"blahblahblah",
		"strategyName":"strategy1",
		"strategyOrderNumber":1,
		"instrumentType":"STK",
		"symbol":"AAPL",
		"currency":"USD",
		"action":"Long",
		"quantity":10
	},
	"cash_long":{
		"function":"placeOrder",
		"password":"blahblahblah",
		"strategyName":"strategy1",
		"strategyOrderNumber":1,
		"instrumentType":"CASH",
		"symbol":"EUR",
		"currency":"USD",
		"action":"Long",
		"quantity":30000
	},
	"cash_short":{
		"function":"placeOrder",
		"password":"blahblahblah",
		"strategyName":"strategy1",
		"strategyOrderNumber":1,
		"instrumentType":"CASH",
		"symbol":"EUR",
		"currency":"USD",
		"action":"Short",
		"quantity":30000
	}
}

def post_request(command, times = 5):
	for i in range(times):
		if command in API:
			r = requests.post("http://localhost:8080/TmesisAPI", json=API[command])

def main():

	record = []
	api_list = [["stk_long", "stk_short"], ["cash_long", "cash_short"]]
	# set the number of process
	process_number = 10
	#stock = 0, cash = 1
	stk_or_cash = 0
	#long = 0, short = 1
	long_or_short = 0
	
	# Multi-process
	for i in range(process_number):
		process = multiprocessing.Process(target=post_request, args=(api_list[stk_or_cash][long_or_short],))
		process.start()
		record.append(process)

	for process in record:
	   process.join()

if __name__ == "__main__":   
	main()