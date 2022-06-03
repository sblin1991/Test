    public const string FARE_ENSURE_FLIGHT_IS_PRESENT_SELECTOR = "self::Fare[count(Segment/SegmentOption/Flight)>0]";
		public const string FARE_HAS_CPP_OR_METOO_SELECTOR = "self::Fare/Segment/SegmentOption/Flight[substring(FareBasisCode, 2, 2) = 'CA' or substring(FareBasisCode, 2, 2) = 'CB' or substring(FareBasisCode, 2, 2) = 'CP' or substring(FareBasisCode, 2, 2) = 'DG']";
		public const string FARE_CPP_OR_METOO_ECONOMY_FLIGHT_SELECTOR = "self::Fare/Segment/SegmentOption/Flight[substring(FareBasisCode, 2, 2) = 'CA' or (substring(FareBasisCode, 2, 2) = 'DG' and FltClass = 'Y')][1]";
		public const string FARE_XCP_OR_METOO_PREMIUM_ECON_FLIGHT_SELECTOR = "self::Fare/Segment/SegmentOption/Flight[substring(FareBasisCode, 2, 2) = 'CP' or (substring(FareBasisCode, 2, 2) = 'DG' and FltClass = 'W')][1]";
		public const string FARE_CPP_OR_METOO_BUSINESS_FLIGHT_SELECTOR = "self::Fare/Segment/SegmentOption/Flight[substring(FareBasisCode, 2, 2) = 'CB' or (substring(FareBasisCode, 2, 2) = 'DG' and FltClass = 'C')][1]";

		public const string ITIN_CPP_OR_METOO_ECONOMY_FLIGHT_SELECTOR = "CliqbookItineraryDisplay/Segments/FlightSeg[substring(FareCode, 2, 2) = 'CA' or (substring(FareCode, 2, 2) = 'DG' and (CabinCode = 'Y' or CabinDesc/OT_BIC/@cabin = 'Y'))][1]";
		public const string ITIN_CPP_OR_METOO_PREMIUM_ECON_FLIGHT_SELECTOR = "CliqbookItineraryDisplay/Segments/FlightSeg[substring(FareCode, 2, 2) = 'CP' or (substring(FareCode, 2, 2) = 'DG' and (CabinCode = 'W' or CabinDesc/OT_BIC/@cabin = 'W'))][1]";
		public const string ITIN_CPP_OR_METOO_BUSINESS_FLIGHT_SELECTOR = "CliqbookItineraryDisplay/Segments/FlightSeg[substring(FareCode, 2, 2) = 'CB' or (substring(FareCode, 2, 2) = 'DG' and (CabinCode = 'C' or CabinDesc/OT_BIC/@cabin = 'C'))][1]";

		
		public DateTime? GetGsaCityPairLastTicketDateFromFare(XElement fareNode, DateTime bookingDateTimeUtc, bool fakeSouthwestLdtpEnabled = false)
		{
			DateTime? returnValue = null;
			DateTime? lastTicketDateEconomy = null;
			DateTime? lastTicketDateBusiness = null;
			DateTime? lastTicketDatePremiumEcon = null;

			try
			{
				// Get Economy and Business LDTP dates if avail and return earliest
				var firstFlightNode = fareNode.XPathSelectElement(FLIGHT_NODE_SELECTOR);
				// XPathSelectElement returns FirstOrDefault() so these will get the first flight with the fare basis
				var flightNodeEconomy = fareNode.XPathSelectElement(FARE_CPP_OR_METOO_ECONOMY_FLIGHT_SELECTOR);
				var flightNodeBusiness = fareNode.XPathSelectElement(FARE_CPP_OR_METOO_BUSINESS_FLIGHT_SELECTOR);
				var flightNodePremiumEcon = fareNode.XPathSelectElement(FARE_XCP_OR_METOO_PREMIUM_ECON_FLIGHT_SELECTOR);

				var carrier = XmlUtils.GetXmlNodeText(firstFlightNode, "Carrier");

				if (fakeSouthwestLdtpEnabled && carrier.Equals("WN"))
				{

					var departDateLocal = DateTime.Parse(XmlUtils.GetXmlNodeText(firstFlightNode, "DepDateTime"));
					var depAirport = XmlUtils.GetXmlNodeText(firstFlightNode, "DepAirp");
					var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);

					var dateDiffInHours = departDateTimeUtc.Subtract(bookingDateTimeUtc).TotalHours;
					var southWestFareNode = fareNode.XPathSelectElement("self::Fare[number(@govFareTypeCode) = " + ((int)GovernmentFareType.GovernmentContractBusiness).ToString() +
																			   " or number(@govFareTypeCode) = " + ((int)GovernmentFareType.GovernmentContractDiscounted).ToString() +
																			   " or number(@govFareTypeCode) = " + ((int)GovernmentFareType.GovernmentContractPremiumEcon).ToString() +
																			   " or number(@govFareTypeCode) = " + ((int)GovernmentFareType.GovernmentContract).ToString() + "]");

					if (dateDiffInHours >= 72 && southWestFareNode != null)
					{
						returnValue = departDateTimeUtc.AddDays(-2.0);
					}
					else
					{
						m_tmtLogger.LogMessage("dateDiffInHours less than 72h or southWestFareNode was not set. Unable to calculate last ticket date", "AirExchange", "GSA Last Ticket Date", "AirHelper");
					}
				}
				else
				{
					if (flightNodeEconomy != null)
					{
						carrier = XmlUtils.GetXmlNodeText(flightNodeEconomy, "Carrier");
						var departDateLocal = DateTime.Parse(XmlUtils.GetXmlNodeText(flightNodeEconomy, "DepDateTime"));
						var depAirport = XmlUtils.GetXmlNodeText(flightNodeEconomy, "DepAirp");
						var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
						lastTicketDateEconomy = GetGsaCityPairLastTicketDate(bookingDateTimeUtc, departDateTimeUtc, carrier);
					}
					if (flightNodePremiumEcon != null)
                    {
						var departDateLocal = DateTime.Parse(XmlUtils.GetXmlNodeText(flightNodePremiumEcon, "DepDateTime"));
						var depAirport = XmlUtils.GetXmlNodeText(flightNodePremiumEcon, "DepAirp");
						var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
						lastTicketDatePremiumEcon = GetGsaCityPairBusinessLastTicketDate(bookingDateTimeUtc, departDateTimeUtc);
					}
					if (flightNodeBusiness != null)
					{
						var departDateLocal = DateTime.Parse(XmlUtils.GetXmlNodeText(flightNodeBusiness, "DepDateTime"));
						var depAirport = XmlUtils.GetXmlNodeText(flightNodeBusiness, "DepAirp");
						var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
						lastTicketDateBusiness = GetGsaCityPairBusinessLastTicketDate(bookingDateTimeUtc, departDateTimeUtc);
					}
					if (lastTicketDateBusiness == null && lastTicketDatePremiumEcon == null && lastTicketDateEconomy != null)
					{
						returnValue = lastTicketDateEconomy;
					}
					else if (lastTicketDatePremiumEcon == null && lastTicketDateEconomy == null && lastTicketDateBusiness != null)
                    {
						returnValue = lastTicketDateBusiness;
                    }
					else if (lastTicketDateBusiness == null && lastTicketDateEconomy == null && lastTicketDatePremiumEcon != null)
                    {
						returnValue = lastTicketDatePremiumEcon;
                    }
					else if (lastTicketDatePremiumEcon == null && lastTicketDateEconomy != null && lastTicketDateBusiness != null)
                    {
						returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDateEconomy.Value) <= 0 ? lastTicketDateBusiness : lastTicketDateEconomy;
					}
					else if (lastTicketDateEconomy == null && lastTicketDateBusiness != null && lastTicketDatePremiumEcon != null)
					{
						returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateBusiness : lastTicketDatePremiumEcon;
					}
					else if (lastTicketDateBusiness == null && lastTicketDateEconomy != null && lastTicketDatePremiumEcon != null)
                    {
						returnValue = DateTime.Compare(lastTicketDateEconomy.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateEconomy : lastTicketDatePremiumEcon;
					}
					else if (lastTicketDateBusiness != null && lastTicketDateEconomy != null && lastTicketDatePremiumEcon != null)
					{
						//In this case you have all three fares types in a node you need to compare all three dates and get the earliest one.
						//Compare the Premium Economy with the Business class fare depending on the departure date we should have the most restrictive amoungst the batch
						returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateBusiness : lastTicketDatePremiumEcon;
						//Compare the earliest 7 day LTD with the Government Economy which is 2 days and return the earliest
						returnValue = DateTime.Compare(lastTicketDateEconomy.Value, returnValue.Value) <= 0 ? lastTicketDateEconomy : returnValue;
					}
					else
                    {
						returnValue = null;
                    }
				}
			}
			catch (Exception exception)
			{
				m_tmtLogger.LogError(exception, "AirExchange", "GetGsaCityPairLastTicketDateFromFare", true, "GSA Last Ticket Date not set. Falling back to non-GSA calculations.");
			}
			return returnValue;
		}
    public DateTime? GetGsaCityPairLastTicketDateFromItin(XDocument itinXmlDoc, DateTime bookingDateTimeUtc)
		{
			DateTime? returnValue = null;
			DateTime? lastTicketDateEconomy = null;
			DateTime? lastTicketDateBusiness = null;
			DateTime? lastTicketDatePremiumEcon = null;
			try
			{
				// Get Economy and Business LDTP dates if avail and return earliest
				// XPathSelectElement returns FirstOrDefault() so these will get the first flight with the fare basis
				// ITIN_HAS_CPP_OR_METOO_SELECTOR
				var flightNodeEconomy = itinXmlDoc.XPathSelectElement(ITIN_CPP_OR_METOO_ECONOMY_FLIGHT_SELECTOR);
				var flightNodeBusiness = itinXmlDoc.XPathSelectElement(ITIN_CPP_OR_METOO_BUSINESS_FLIGHT_SELECTOR);
				var flightNodePremiumEcon = itinXmlDoc.XPathSelectElement(ITIN_CPP_OR_METOO_PREMIUM_ECON_FLIGHT_SELECTOR);

				if (flightNodeEconomy != null)
				{

					var departDate = XmlUtils.GetXmlNodeText(flightNodeEconomy, "DepartDate");
					var departTime = XmlUtils.GetXmlNodeText(flightNodeEconomy, "DepartTime");
					var departDateLocal = DateTime.Parse(departDate + " " + departTime);
					var carrier = XmlUtils.GetXmlNodeText(flightNodeEconomy, "CarrierCode");
					var depAirport = XmlUtils.GetXmlNodeText(flightNodeEconomy, "DepartCity");
					var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
					lastTicketDateEconomy = GetGsaCityPairLastTicketDate(bookingDateTimeUtc, departDateTimeUtc, carrier);
				}
				if(flightNodePremiumEcon != null)
				{
					var departDate = XmlUtils.GetXmlNodeText(flightNodePremiumEcon, "DepartDate");
					var departTime = XmlUtils.GetXmlNodeText(flightNodePremiumEcon, "DepartTime");
					var departDateLocal = DateTime.Parse(departDate + " " + departTime);
					var depAirport = XmlUtils.GetXmlNodeText(flightNodePremiumEcon, "DepartCity");
					var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
					lastTicketDatePremiumEcon = GetGsaCityPairBusinessLastTicketDate(bookingDateTimeUtc, departDateTimeUtc);
				}
				if (flightNodeBusiness != null)
				{
					var departDate = XmlUtils.GetXmlNodeText(flightNodeBusiness, "DepartDate");
					var departTime = XmlUtils.GetXmlNodeText(flightNodeBusiness, "DepartTime");
					var departDateLocal = DateTime.Parse(departDate + " " + departTime);
					var depAirport = XmlUtils.GetXmlNodeText(flightNodeBusiness, "DepartCity");
					var departDateTimeUtc = GetAirportDateTimeUtc(departDateLocal, depAirport);
					lastTicketDateBusiness = GetGsaCityPairBusinessLastTicketDate(bookingDateTimeUtc, departDateTimeUtc);
				}
				if (lastTicketDateBusiness == null && lastTicketDatePremiumEcon == null && lastTicketDateEconomy != null)
				{
					returnValue = lastTicketDateEconomy;
				}
				else if (lastTicketDatePremiumEcon == null && lastTicketDateEconomy == null && lastTicketDateBusiness != null)
				{
					returnValue = lastTicketDateBusiness;
				}
				else if (lastTicketDateBusiness == null && lastTicketDateEconomy == null && lastTicketDatePremiumEcon != null)
				{
					returnValue = lastTicketDatePremiumEcon;
				}
				else if (lastTicketDatePremiumEcon == null && lastTicketDateEconomy != null && lastTicketDateBusiness != null)
				{
					returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDateEconomy.Value) <= 0 ? lastTicketDateBusiness : lastTicketDateEconomy;
				}
				else if (lastTicketDateEconomy == null && lastTicketDateBusiness != null && lastTicketDatePremiumEcon != null)
				{
					returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateBusiness : lastTicketDatePremiumEcon;
				}
				else if (lastTicketDateBusiness == null && lastTicketDateEconomy != null && lastTicketDatePremiumEcon != null)
				{
					returnValue = DateTime.Compare(lastTicketDateEconomy.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateEconomy : lastTicketDatePremiumEcon;
				}
				else if (lastTicketDateBusiness != null && lastTicketDateEconomy != null && lastTicketDatePremiumEcon != null)
				{
					//In this case you have all three fares types in a node you need to compare all three dates and get the earliest one.
					//Compare the Premium Economy with the Business class fare depending on the departure date we should have the most restrictive amoungst the batch
					returnValue = DateTime.Compare(lastTicketDateBusiness.Value, lastTicketDatePremiumEcon.Value) <= 0 ? lastTicketDateBusiness : lastTicketDatePremiumEcon;
					//Compare the earliest 7 day LTD with the Government Economy which is 2 days and return the earliest
					returnValue = DateTime.Compare(lastTicketDateEconomy.Value, returnValue.Value) <= 0 ? lastTicketDateEconomy : returnValue;
				} 
				else
                {
					returnValue = null;
                }
			}
			catch (Exception exception)
			{
				m_tmtLogger.LogError(exception, "AirExchange", "GetGsaCityPairLastTicketDateFromItin", true, "GSA Last Ticket Date not set. Falling back to non-GSA calculations.");
			}
			return returnValue;
		}
