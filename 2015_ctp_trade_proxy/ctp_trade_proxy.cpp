// ctp_trade_proxy.cpp : 定义 DLL 应用程序的导出函数。
//

#include "ctp_trade_proxy.h"
#include "../2015_include_lib/HFTrade.h"

CctpTrade *spi;
CThostFtdcTraderApi *api;
map<string, InstrumentField> _id_instrument;
map<long, string> _id_sysid; //OrderID&sysid


DllExport void WINAPI CreateApi()
{
	api = CThostFtdcTraderApi::CreateFtdcTraderApi("./log/");

}
DllExport int WINAPI ReqConnect(char *pFront)
{
	spi = new CctpTrade();
	api->RegisterFront(pFront);
	api->RegisterSpi(spi);
	api->SubscribePrivateTopic(THOST_TERT_QUICK);	//私有流用quick
	api->SubscribePublicTopic(THOST_TERT_RESTART);	//公有流用public
	api->Init();
	return 0;
}
DllExport int WINAPI ReqUserLogin(char* pInvestor, char* pPwd, char* pBroker)
{
	CThostFtdcReqUserLoginField f;
	memset(&f, 0, sizeof(CThostFtdcReqUserLoginField));
	strcpy_s(f.BrokerID, sizeof(f.BrokerID), pBroker);
	strcpy_s(f.UserID, sizeof(f.UserID), pInvestor);
	strcpy_s(f.Password, sizeof(f.Password), pPwd);
	strcpy_s(f.UserProductInfo, "@Haifeng");
	strcpy_s(_broker, f.BrokerID);
	strcpy_s(_investor, f.UserID);
	strcpy_s(_TradingDay, "");
	return api->ReqUserLogin(&f, ++req);
}
DllExport void WINAPI ReqUserLogout()
{
	_session = 0;
	api->RegisterSpi(NULL);
	api->Release();
}
DllExport const char* WINAPI GetTradingDay()
{
	if (_TradingDay || strlen(_TradingDay) == 0)
	{
		strcpy_s(_TradingDay, api->GetTradingDay());
	}
	return _TradingDay;
}

//DllExport int WINAPI ReqQryOrder()
int QryOrder()
{
	/*if (_started)
	{
	if (_OnRspQryOrder)
	{
	for (map<long, OrderField>::iterator i = _id_order.begin(); i != _id_order.end(); ++i)
	{
	((DefOnRspQryOrder)_OnRspQryOrder)(&i->second, i == _id_order.end());
	}
	}
	return;
	}*/
	CThostFtdcQryOrderField f;
	memset(&f, 0, sizeof(CThostFtdcQryOrderField));
	strcpy_s(f.BrokerID, _broker);
	strcpy_s(f.InvestorID, _investor);
	return api->ReqQryOrder(&f, ++req);
}
int QryTrade()
{
	CThostFtdcQryTradeField f;
	memset(&f, 0, sizeof(CThostFtdcQryTradeField));
	strcpy_s(f.BrokerID, _broker);
	strcpy_s(f.InvestorID, _investor);
	return api->ReqQryTrade(&f, ++req);
}
DllExport int WINAPI ReqQryPosition()
{
	CThostFtdcQryInvestorPositionField f;
	memset(&f, 0, sizeof(CThostFtdcQryInvestorPositionField));
	strcpy_s(f.BrokerID, _broker);
	strcpy_s(f.InvestorID, _investor);
	return api->ReqQryInvestorPosition(&f, ++req);
}
DllExport int WINAPI ReqQryAccount()
{
	CThostFtdcQryTradingAccountField f;
	memset(&f, 0, sizeof(CThostFtdcQryTradingAccountField));
	strcpy_s(f.BrokerID, _broker);
	strcpy_s(f.InvestorID, _investor);
	return api->ReqQryTradingAccount(&f, ++req);
}

DllExport int WINAPI ReqOrderInsert(char *pInstrument, DirectionType pDirection, OffsetType pOffset, double pPrice, int pVolume, HedgeType pHedge, OrderType pType, char* pCustom)
{
	CThostFtdcInputOrderField f;
	memset(&f, 0, sizeof(CThostFtdcInputOrderField));

	strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pInstrument);
	strcpy_s(f.BrokerID, sizeof(f.BrokerID), _broker);
	switch (pHedge)
	{
	case  Speculation:
		f.CombHedgeFlag[0] = THOST_FTDC_HF_Speculation;
		break;
	case  Arbitrage:
		f.CombHedgeFlag[0] = THOST_FTDC_HF_Arbitrage;
		break;
	case  Hedge:
		f.CombHedgeFlag[0] = THOST_FTDC_HF_Hedge;
		break;
	}
	switch (pDirection)
	{
	case Buy:
		f.Direction = THOST_FTDC_D_Buy;
		break;
	default:
		f.Direction = THOST_FTDC_D_Sell;
		break;
	}
	switch (pOffset)
	{
	case Open:
		f.CombOffsetFlag[0] = THOST_FTDC_OF_Open;
		break;
	case CloseToday:
		f.CombOffsetFlag[0] = THOST_FTDC_OF_CloseToday;
		break;
	case  Close:
		f.CombOffsetFlag[0] = THOST_FTDC_OF_Close;
		break;
	}
	f.VolumeTotalOriginal = pVolume;
	strcpy_s(f.InvestorID, sizeof(f.InvestorID), _investor);
	f.IsAutoSuspend = 0;

	f.ContingentCondition = THOST_FTDC_CC_Immediately;
	f.ForceCloseReason = THOST_FTDC_FCC_NotForceClose;
	f.IsSwapOrder = 0;
	f.UserForceClose = 0;

	f.OrderPriceType = THOST_FTDC_OPT_LimitPrice;
	f.VolumeCondition = THOST_FTDC_VC_AV;
	f.TimeCondition = THOST_FTDC_TC_IOC;
	f.MinVolume = 1;
	f.LimitPrice = pPrice;

	switch (pType)
	{
	case  Limit:
		f.TimeCondition = THOST_FTDC_TC_GFD;
		break;
	case  Market:
		f.OrderPriceType = THOST_FTDC_OPT_AnyPrice;
		f.LimitPrice = 0;
		break;
	case  FAK:
		break;
	case FOK:
		f.VolumeCondition = THOST_FTDC_VC_CV; //全部数量
		break;
	}

	if (pCustom == NULL)
		pCustom = new char('\n');
	string str(pCustom);
	str = str.length() > 6 ? str.substr(0, 6) : (string(6 - str.length(), ' ') + str);
	sprintf_s(f.OrderRef, "%d%s", ++req, str.c_str());
	return api->ReqOrderInsert(&f, req);
}

DllExport int WINAPI ReqOrderAction(long pOrderId)
{
	if (_id_order.find(pOrderId) == _id_order.end()) //不存在
	{
		if (_OnRtnError)
		{
			((DefOnRtnError)_OnRtnError)(pOrderId, "OrderActionError:no OrderID.");
		}
		return -1;
	}
	if (_id_sysid.find(pOrderId) == _id_sysid.end())
	{
		Sleep(100); //增加等待返回时间
		if (_id_sysid.find(pOrderId) == _id_sysid.end())
		{
			if (_OnRtnError)
			{
				((DefOnRtnError)_OnRtnError)(pOrderId, "OrderActionError:no sysid.");
			}
			return -1;
		}
	}

	OrderField of = _id_order[pOrderId];// *iter->second;// ._id_order.find(pOrderId);
	if (_id_instrument.find(of.InstrumentID) == _id_instrument.end())
	{
		if (_OnRtnError)
		{
			((DefOnRtnError)_OnRtnError)(pOrderId, "OrderActionError:no instrumentid.");
		}
		return -1;
	}
	CThostFtdcInputOrderActionField f;
	memset(&f, 0, sizeof(CThostFtdcInputOrderActionField));
	f.ActionFlag = THOST_FTDC_AF_Delete;
	strcpy_s(f.BrokerID, sizeof(f.BrokerID), _broker);
	strcpy_s(f.InvestorID, sizeof(f.InvestorID), _investor);
	strcpy_s(f.ExchangeID, _id_instrument[of.InstrumentID].ExchangeID);
	strcpy_s(f.OrderSysID, sizeof(f.OrderSysID), _id_sysid[pOrderId].c_str());
	return api->ReqOrderAction(&f, ++req);
}

void CctpTrade::OnFrontConnected()
{
	if (_OnFrontConnected)
	{
		((DefOnFrontConnected)_OnFrontConnected)();
	}
}

void CctpTrade::OnFrontDisconnected(int nReason)
{
	if (_OnRspUserLogout)
	{
		((DefOnRspUserLogout)_OnRspUserLogout)(nReason);
	}
}

void CctpTrade::OnRspUserLogin(CThostFtdcRspUserLoginField *pRspUserLogin, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRspUserLogin)
	{
		_session = pRspUserLogin->SessionID;
		CThostFtdcSettlementInfoConfirmField f;
		memset(&f, 0, sizeof(CThostFtdcSettlementInfoConfirmField));
		strcpy_s(f.BrokerID, sizeof(f.BrokerID), _broker);
		strcpy_s(f.InvestorID, sizeof(f.InvestorID), _investor);
		if (pRspInfo->ErrorID == 0)
			api->ReqSettlementInfoConfirm(&f, ++req);
		((DefOnRspUserLogin)_OnRspUserLogin)(pRspInfo == NULL ? 0 : pRspInfo->ErrorID);
	}
}

void CctpTrade::OnRspSettlementInfoConfirm(CThostFtdcSettlementInfoConfirmField *pSettlementInfoConfirm, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (pRspInfo->ErrorID != 0) return;

	CThostFtdcQryInstrumentField f;
	memset(&f, 0, sizeof(CThostFtdcQryInstrumentField));
	api->ReqQryInstrument(&f, ++req);
}
void QryAccount()
{
	while (_session != 0)
	{
		ReqQryAccount();
		Sleep(1100);
	}
}
void CctpTrade::OnRspQryInstrument(CThostFtdcInstrumentField *pInstrument, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (pInstrument)
	{
		InstrumentField f;
		memset(&f, 0, sizeof(InstrumentField));
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pInstrument->InstrumentID);
		strcpy_s(f.ExchangeID, sizeof(f.ExchangeID), pInstrument->ExchangeID);
		f.PriceTick = pInstrument->PriceTick;
		f.VolumeMultiple = pInstrument->VolumeMultiple;
		strcpy_s(f.ProductID, sizeof(f.ProductID), pInstrument->ProductID);
		switch (pInstrument->ProductClass)
		{
		case THOST_FTDC_PC_Futures:
			f.ProductClass = Futures;
			break;
		case THOST_FTDC_PC_Options:
			f.ProductClass = Options;
			break;
		case THOST_FTDC_PC_Combination:
			f.ProductClass = Combination;
			break;
		case THOST_FTDC_PC_SpotOption:
			f.ProductClass = SpotOption;
			break;
		default:
			f.ProductClass = Futures;
			break;
		}
		_id_instrument[string(f.InstrumentID)] = f;

		if (_OnRspQryInstrument)
		{
			((DefOnRspQryInstrument)_OnRspQryInstrument)(&f, bIsLast);
		}
	}
	if (bIsLast)
	{
		Sleep(1100);
		if (_session == 0)
			return;
		ReqQryAccount();
	}
}

void CctpTrade::OnRspQryTradingAccount(CThostFtdcTradingAccountField *pTradingAccount, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRspQryTradingAccount)
	{
		TradingAccount f;
		memset(&f, 0, sizeof(TradingAccount));
		if (pTradingAccount)
		{
			f.Available = pTradingAccount->Available;
			f.CloseProfit = pTradingAccount->CloseProfit + pTradingAccount->OptionCloseProfit;
			f.Commission = pTradingAccount->Commission;
			f.CurrMargin = pTradingAccount->CurrMargin;
			f.FrozenCash = pTradingAccount->FrozenCash;
			f.PositionProfit = pTradingAccount->PositionProfit + pTradingAccount->OptionValue;
			f.PreBalance = pTradingAccount->PreBalance;
			f.Fund = f.PreBalance + f.CloseProfit + f.PositionProfit + pTradingAccount->Deposit - pTradingAccount->Withdraw+
				pTradingAccount->CashIn;
		}
		((DefOnRspQryTradingAccount)_OnRspQryTradingAccount)(&f);
	}
	if (bIsLast && !_started)
	{
		Sleep(1100);
		if (_session == 0)
			return;
		ReqQryPosition();
	}
}

void CctpTrade::OnRspQryInvestorPosition(CThostFtdcInvestorPositionField *pInvestorPosition, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRspQryPosition)
	{
		PositionField f;
		memset(&f, 0, sizeof(PositionField));
		if (pInvestorPosition)
		{
			switch (pInvestorPosition->HedgeFlag)
			{
			case  THOST_FTDC_HF_Speculation:
				f.Hedge = Speculation;
				break;
			case  THOST_FTDC_HF_Arbitrage:
				f.Hedge = Arbitrage;
				break;
			case  THOST_FTDC_HF_Hedge:
				f.Hedge = Hedge;
				break;
			}
			switch (pInvestorPosition->PosiDirection)
			{
			case THOST_FTDC_PD_Long:
				f.Direction = Buy;
				break;
			default:
				f.Direction = Sell;
				break;
			}

			strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pInvestorPosition->InstrumentID);
			//f.Margin = pInvestorPosition->UseMargin;
			f.Price = pInvestorPosition->Position == 0 ? 0 : (pInvestorPosition->PositionCost / _id_instrument[pInvestorPosition->InstrumentID].VolumeMultiple / pInvestorPosition->Position);
			f.Position = pInvestorPosition->Position;
			f.TdPosition = pInvestorPosition->TodayPosition;
			f.YdPosition = f.Position - f.TdPosition; //pInvestorPosition->YdPosition; 平仓后不知如何计算
		}
		((DefOnRspQryPosition)_OnRspQryPosition)(&f, bIsLast);
	}
	if (bIsLast && !_started)
	{
		Sleep(1100);
		if (_session == 0)
			return;
		ReqQryOrder();
	}
}

void CctpTrade::OnRspQryOrder(CThostFtdcOrderField *pOrder, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	OrderField f;
	memset(&f, 0, sizeof(OrderField));
	if (pOrder)
	{
		long id = atol(pOrder->OrderLocalID);
		if (_id_order.find(id) == _id_order.end())
		{
			//f.AvgPrice = pOrder
			f.Direction = pOrder->Direction == THOST_FTDC_D_Buy ? Buy : Sell;
			switch (pOrder->CombHedgeFlag[0])
			{
			case  THOST_FTDC_HF_Speculation:
				f.Hedge = Speculation;
				break;
			case  THOST_FTDC_HF_Arbitrage:
				f.Hedge = Arbitrage;
				break;
			case  THOST_FTDC_HF_Hedge:
				f.Hedge = Hedge;
				break;
			}
			switch (pOrder->Direction)
			{
			case THOST_FTDC_D_Buy:
				f.Direction = Buy;
				break;
			default:
				f.Direction = Sell;
				break;
			}
			switch (pOrder->CombOffsetFlag[0])
			{
			case THOST_FTDC_OF_Open:
				f.Offset = Open;
				break;
			case THOST_FTDC_OF_CloseToday:
				f.Offset = CloseToday;
				break;
			case  THOST_FTDC_OF_Close:
				f.Offset = Close;
				break;
			}
			strcpy_s(f.InsertTime, sizeof(f.InsertTime), pOrder->InsertTime);
			strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pOrder->InstrumentID);
			//strcpy_s(f.TradeTime, sizeof(f.TradeTime), pOrder->UpdateTime);
			f.IsLocal = pOrder->SessionID == _session;
			f.LimitPrice = pOrder->LimitPrice;
			//即时返回委托编号,由上层处理
			//if (strlen(pOrder->OrderRef) == 9) //以hhmmssfff格式的标识
			//	f.OrderID = atoi(pOrder->OrderRef);
			//else
			f.OrderID = atoi(pOrder->OrderLocalID);
			switch (pOrder->OrderStatus)
			{
			case THOST_FTDC_OST_Canceled:
				f.Status = Canceled;
				break;
			case THOST_FTDC_OST_AllTraded:
				f.Status = Filled;
				break;
			case THOST_FTDC_OST_PartTradedQueueing:
				f.Status = Partial;
				break;
			default:
				f.Status = Normal;
				break;
			}
			f.Volume = pOrder->VolumeTotalOriginal;

			f.VolumeLeft = f.Volume;	//需要计算均价用//2014.9.9注销

			int len = string(pOrder->OrderRef).length(); //2014.9.22增加custom
			if (len > 6)
			{
				for (int i = 0; i < 6; ++i)
				{
					f.Custom[i] = pOrder->OrderRef[len - 6 + i];
				}
			}
			_id_order[f.OrderID] = f;
		}
		else
			f = _id_order[id];
		//修复:重启后无法撤单
		if (pOrder->OrderSysID)// && strlen(pOrder->OrderSysID) > 0)
		{
			_id_sysid[id] = string(pOrder->OrderSysID);  //撤单用
		}
	}
	/*if (_OnRspQryOrder)
	{
	((DefOnRspQryOrder)_OnRspQryOrder)(&f, bIsLast);
	}*/
	if (bIsLast && !_started)
	{
		Sleep(1100);
		if (_session == 0)
			return;
		ReqQryTrade();
	}
}

void CctpTrade::OnRspQryTrade(CThostFtdcTradeField *pTrade, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	TradeField f;
	memset(&f, 0, sizeof(TradeField));
	if (pTrade)
	{
		long id = atol(pTrade->OrderLocalID);

		if (_id_order.find(id) != _id_order.end())
		{
			OrderField f = _id_order[id];
			f.AvgPrice = ((f.AvgPrice*(f.Volume - f.VolumeLeft)) + pTrade->Price*pTrade->Volume) / (f.Volume - f.VolumeLeft + pTrade->Volume);
			strcpy_s(f.TradeTime, 16, pTrade->TradeTime);
			_id_order[id] = f;
			//f.TradeVolume = pTrade->Volume;
			//f.VolumeLeft -= f.TradeVolume;
			//((DefOnRtnOrder)_OnRtnOrder)(&f);
		}

		f.OrderID = id;

		switch (pTrade->HedgeFlag)
		{
		case  THOST_FTDC_HF_Speculation:
			f.Hedge = Speculation;
			break;
		case  THOST_FTDC_HF_Arbitrage:
			f.Hedge = Arbitrage;
			break;
		case  THOST_FTDC_HF_Hedge:
			f.Hedge = Hedge;
			break;
		}
		switch (pTrade->Direction)
		{
		case THOST_FTDC_D_Buy:
			f.Direction = Buy;
			break;
		default:
			f.Direction = Sell;
			break;
		}
		switch (pTrade->OffsetFlag)
		{
		case THOST_FTDC_OF_Open:
			f.Offset = Open;
			break;
		case THOST_FTDC_OF_CloseToday:
			f.Offset = CloseToday;
			break;
		case  THOST_FTDC_OF_Close:
			f.Offset = Close;
			break;
		}
		strcpy_s(f.ExchangeID, sizeof(f.ExchangeID), pTrade->ExchangeID);
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pTrade->InstrumentID);
		f.OrderID = atol(pTrade->OrderLocalID);
		f.Price = pTrade->Price;
		strcpy_s(f.TradeID, sizeof(f.TradeID), pTrade->TradeID);
		strcpy_s(f.TradeTime, sizeof(f.TradeTime), pTrade->TradeTime);
		strcpy_s(f.TradingDay, sizeof(f.TradingDay), pTrade->TradingDay);
		f.Volume = pTrade->Volume;
		char tid[128];
		sprintf_s(tid, "%s%d", f.TradeID, f.Direction);
		_id_trade[string(tid)] = f;
	}
	/*if (_OnRspQryTrade)
	{
	((DefOnRspQryTrade)_OnRspQryTrade)(&f, bIsLast);
	}*/
	if (bIsLast && !_started)
	{
		_started = true;
		hThread = CreateThread(
			NULL,                                   // SD  
			0,                                  // initial stack size  
			(LPTHREAD_START_ROUTINE)QryOnLaunch,    // thread function  
			NULL,                                    // thread argument  
			0,                                   // creation option  
			NULL//threadID                               // thread identifier  
			);
	}
}

void CctpTrade::OnRtnInstrumentStatus(CThostFtdcInstrumentStatusField *pInstrumentStatus)
{
	if (_OnRtnExchangeStatus)
	{
		ExchangeStatusType status = BeforeTrading;
		switch (pInstrumentStatus->InstrumentStatus)
		{
			/*case  THOST_FTDC_IS_AuctionBalance:
				case  THOST_FTDC_IS_AuctionMatch:
				case THOST_FTDC_IS_BeforeTrading:*/
		case THOST_FTDC_IS_Continous:
			status = Trading;
			break;
		case THOST_FTDC_IS_Closed:
			status = Closed;
			break;
		case THOST_FTDC_IS_NoTrading:
			status = NoTrading;
			break;
		}
		((DefOnRtnExchangeStatus)_OnRtnExchangeStatus)(pInstrumentStatus->InstrumentID, status);
	}
}

void CctpTrade::OnRspError(CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRtnError && pRspInfo)
	{
		((DefOnRtnError)_OnRtnError)(pRspInfo->ErrorID, pRspInfo->ErrorMsg);
	}
}

void CctpTrade::OnRtnTradingNotice(CThostFtdcTradingNoticeInfoField *pTradingNoticeInfo)
{
	if (_OnRtnNotice)
	{
		((DefOnRtnNotice)_OnRtnNotice)(pTradingNoticeInfo->FieldContent);
	}
}


void CctpTrade::OnRtnOrder(CThostFtdcOrderField *pOrder)
{
	long id = atol(pOrder->OrderLocalID);
	OrderField f;
	if (_id_order.find(id) == _id_order.end())
	{
		memset(&f, 0, sizeof(OrderField));

		switch (pOrder->CombHedgeFlag[0])
		{
		case  THOST_FTDC_HF_Speculation:
			f.Hedge = Speculation;
			break;
		case  THOST_FTDC_HF_Arbitrage:
			f.Hedge = Arbitrage;
			break;
		case  THOST_FTDC_HF_Hedge:
			f.Hedge = Hedge;
			break;
		}
		switch (pOrder->Direction)
		{
		case THOST_FTDC_D_Buy:
			f.Direction = Buy;
			break;
		default:
			f.Direction = Sell;
			break;
		}
		switch (pOrder->CombOffsetFlag[0])
		{
		case THOST_FTDC_OF_Open:
			f.Offset = Open;
			break;
		case THOST_FTDC_OF_CloseToday:
			f.Offset = CloseToday;
			break;
		case  THOST_FTDC_OF_Close:
			f.Offset = Close;
			break;
		}
		strcpy_s(f.InsertTime, sizeof(f.InsertTime), pOrder->InsertTime);
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pOrder->InstrumentID);
		//strcpy_s(f.TradeTime, sizeof(f.TradeTime), pOrder->UpdateTime);
		f.IsLocal = pOrder->SessionID == _session;
		f.LimitPrice = pOrder->LimitPrice;
		f.OrderID = id;
		f.Volume = pOrder->VolumeTotalOriginal;
		f.VolumeLeft = f.Volume;// pOrder->VolumeTotal;
		//f->VolumeLeft = pOrder->VolumeTotal; //由ontrade处理
		f.Status = Normal;

		int len = string(pOrder->OrderRef).length();
		if (len > 6)
		{
			for (int i = 0; i < 6; ++i)
			{
				f.Custom[i] = pOrder->OrderRef[len - 6 + i];
			}
		}

		if (_OnRtnOrder)
		{
			((DefOnRtnOrder)_OnRtnOrder)(&f);
		}
	}
	else
		f = _id_order[id];

	switch (pOrder->OrderStatus)
	{
	case THOST_FTDC_OST_Canceled:
		f.Status = Canceled;
		break;
	case THOST_FTDC_OST_AllTraded:
		f.Status = Filled;
		break;
	case THOST_FTDC_OST_PartTradedQueueing:
		f.Status = Partial;
		break;
	default:
		f.Status = Normal;
		break;
	}
	if (pOrder->OrderSysID)// && strlen(pOrder->OrderSysID) > 0)
	{
		_id_sysid[id] = string(pOrder->OrderSysID);  //撤单用
	}

	_id_order[id] = f; //数据更新

	if (f.Status == Canceled)
	{
		if (_OnRtnCancel)
			((DefOnRtnCancel)_OnRtnCancel)(&f);
		if (_OnRtnError && strstr(pOrder->StatusMsg, "被拒绝") != NULL)
		{
			char msg[512];
			sprintf_s(msg, "OrderInsertError(id:%d):%s", f.OrderID, pOrder->StatusMsg);
			((DefOnRtnError)_OnRtnError)(-1, msg);
		}
	}
}

void CctpTrade::OnRtnTrade(CThostFtdcTradeField *pTrade)
{
	long id = atol(pTrade->OrderLocalID);
	if (_id_order.find(id) != _id_order.end())
	{
		OrderField f = _id_order[id];

		strcpy_s(f.TradeTime, 16, pTrade->TradeTime);
		f.AvgPrice = ((f.AvgPrice*(f.Volume - f.VolumeLeft)) + pTrade->Price*pTrade->Volume) / (f.Volume - f.VolumeLeft + pTrade->Volume);
		f.TradeVolume = pTrade->Volume;
		f.VolumeLeft -= f.TradeVolume;
		if (f.VolumeLeft == 0)
			f.Status = Filled;
		else
			f.Status = Partial;
		_id_order[id] = f;
		if (_OnRtnOrder)
		{
			((DefOnRtnOrder)_OnRtnOrder)(&f);
		}
	}

	TradeField f;
	memset(&f, 0, sizeof(TradeField));
	f.OrderID = id;

	switch (pTrade->HedgeFlag)
	{
	case  THOST_FTDC_HF_Speculation:
		f.Hedge = Speculation;
		break;
	case  THOST_FTDC_HF_Arbitrage:
		f.Hedge = Arbitrage;
		break;
	case  THOST_FTDC_HF_Hedge:
		f.Hedge = Hedge;
		break;
	}
	switch (pTrade->Direction)
	{
	case THOST_FTDC_D_Buy:
		f.Direction = Buy;
		break;
	default:
		f.Direction = Sell;
		break;
	}
	switch (pTrade->OffsetFlag)
	{
	case THOST_FTDC_OF_Open:
		f.Offset = Open;
		break;
	case THOST_FTDC_OF_CloseToday:
		f.Offset = CloseToday;
		break;
	case  THOST_FTDC_OF_Close:
		f.Offset = Close;
		break;
	}
	strcpy_s(f.ExchangeID, sizeof(f.ExchangeID), pTrade->ExchangeID);
	strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pTrade->InstrumentID);
	f.OrderID = atol(pTrade->OrderLocalID);
	f.Price = pTrade->Price;
	strcpy_s(f.TradeID, sizeof(f.TradeID), pTrade->TradeID);
	strcpy_s(f.TradeTime, sizeof(f.TradeTime), pTrade->TradeTime);
	strcpy_s(f.TradingDay, sizeof(f.TradingDay), pTrade->TradingDay);
	f.Volume = pTrade->Volume;
	char tid[128];
	sprintf_s(tid, "%s%d", pTrade->TradeID, pTrade->Direction);
	_id_trade[tid] = f;

	if (_OnRtnTrade)
	{
		((DefOnRtnTrade)_OnRtnTrade)(&f);
	}
}

void CctpTrade::OnRspOrderInsert(CThostFtdcInputOrderField *pInputOrder, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	//ref重复::重发
	if (pRspInfo->ErrorID == 22)
	{
		char custom[8];
		int len = string(pInputOrder->OrderRef).length();
		if (len > 6)
		{
			for (int i = 0; i < 6; ++i)
			{
				custom[i] = pInputOrder->OrderRef[len - 6 + i];
			}
		}
		HedgeType hedge = Speculation;
		switch (pInputOrder->CombHedgeFlag[0])
		{
		case  THOST_FTDC_HF_Speculation:
			hedge = Speculation;
			break;
		case  THOST_FTDC_HF_Arbitrage:
			hedge = Arbitrage;
			break;
		case  THOST_FTDC_HF_Hedge:
			hedge = Hedge;
			break;
		}
		DirectionType dire = Buy;
		switch (pInputOrder->Direction)
		{
		case THOST_FTDC_D_Buy:
			dire = Buy;
			break;
		default:
			dire = Sell;
			break;
		}
		OffsetType offset = Open;
		switch (pInputOrder->CombOffsetFlag[0])
		{
		case THOST_FTDC_OF_Open:
			offset = Open;
			break;
		case THOST_FTDC_OF_CloseToday:
			offset = CloseToday;
			break;
		case  THOST_FTDC_OF_Close:
			offset = Close;
			break;
		}
		OrderType type = Limit;
		if (pInputOrder->TimeCondition == THOST_FTDC_OPT_AnyPrice)
			type = Market;
		else if (pInputOrder->TimeCondition == THOST_FTDC_TC_IOC)
		{
			if (pInputOrder->VolumeCondition = THOST_FTDC_VC_CV)
				type = FAK;
			else
				type = FOK;
		}
		ReqOrderInsert(pInputOrder->InstrumentID, dire, offset, pInputOrder->LimitPrice, pInputOrder->VolumeTotalOriginal, hedge, type, custom);
	}
	else
	{
		OrderField f;
		memset(&f, 0, sizeof(OrderField));

		switch (pInputOrder->CombHedgeFlag[0])
		{
		case  THOST_FTDC_HF_Speculation:
			f.Hedge = Speculation;
			break;
		case  THOST_FTDC_HF_Arbitrage:
			f.Hedge = Arbitrage;
			break;
		case  THOST_FTDC_HF_Hedge:
			f.Hedge = Hedge;
			break;
		}
		switch (pInputOrder->Direction)
		{
		case THOST_FTDC_D_Buy:
			f.Direction = Buy;
			break;
		default:
			f.Direction = Sell;
			break;
		}
		switch (pInputOrder->CombOffsetFlag[0])
		{
		case THOST_FTDC_OF_Open:
			f.Offset = Open;
			break;
		case THOST_FTDC_OF_CloseToday:
			f.Offset = CloseToday;
			break;
		case  THOST_FTDC_OF_Close:
			f.Offset = Close;
			break;
		}
		strcpy_s(f.InsertTime, sizeof(f.InsertTime), "23:59:59");
		strcpy_s(f.InstrumentID, sizeof(f.InstrumentID), pInputOrder->InstrumentID);
		//strcpy_s(f.TradeTime, sizeof(f.TradeTime), pOrder->UpdateTime);
		f.IsLocal = true;// pOrder->SessionID == _session;
		f.LimitPrice = pInputOrder->LimitPrice;
		f.OrderID = req;
		f.Volume = pInputOrder->VolumeTotalOriginal;
		f.VolumeLeft = f.Volume;// pOrder->VolumeTotal;
		//f->VolumeLeft = pOrder->VolumeTotal; //由ontrade处理
		f.Status = Normal;

		int len = string(pInputOrder->OrderRef).length();
		if (len > 6)
		{
			for (int i = 0; i < 6; ++i)
			{
				f.Custom[i] = pInputOrder->OrderRef[len - 6 + i];
			}
		}

		if (_OnRtnOrder)
		{
			((DefOnRtnOrder)_OnRtnOrder)(&f);
		}

		f.Status = Canceled;

		if (_OnRtnCancel)
			((DefOnRtnCancel)_OnRtnCancel)(&f);

		if (_OnRtnError)
		{
			char msg[512];
			sprintf_s(msg, "%s:%s", "OrderInsertError:", pRspInfo->ErrorMsg);
			((DefOnRtnError)_OnRtnError)(pRspInfo == NULL ? -1 : pRspInfo->ErrorID, msg);
		}
	}
}

void CctpTrade::OnErrRtnOrderInsert(CThostFtdcInputOrderField *pInputOrder, CThostFtdcRspInfoField *pRspInfo)
{

}

void CctpTrade::OnRspOrderAction(CThostFtdcInputOrderActionField *pInputOrderAction, CThostFtdcRspInfoField *pRspInfo, int nRequestID, bool bIsLast)
{
	if (_OnRtnError)
	{
		char msg[512];
		sprintf_s(msg, "%s:%s", "OrderActionError:", pRspInfo->ErrorMsg);
		((DefOnRtnError)_OnRtnError)((long)pRspInfo->ErrorID, msg);
	}
}

void CctpTrade::OnErrRtnOrderAction(CThostFtdcOrderActionField *pOrderAction, CThostFtdcRspInfoField *pRspInfo)
{
	if (_OnRtnError)
	{
		char msg[512];
		sprintf_s(msg, "%s:%s", "OrderActionError:", pRspInfo->ErrorMsg);
		((DefOnRtnError)_OnRtnError)(atol(pOrderAction->OrderLocalID), msg);
	}
}

