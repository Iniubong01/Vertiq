using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; 
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Types;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class JupiterSwapManager : MonoBehaviour
{
    [Header("Jupiter Configuration")]
    public string jupiterBaseUrl = "https://api.jup.ag"; 
    public string jupiterApiKey = "YOUR_KEY_HERE"; 

    [Header("Token Configuration")]
    public string wrappedSolMint = "So11111111111111111111111111111111111111112";
    public string usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; 
    public string playTokenMint = "PLAYs3GSSadH2q2JLS7djp7yzeT75NK78XgrE5YLrfq"; 
    
    [Header("UI References")]
    public TMP_InputField inputAmountField;
    public TMP_InputField outputAmountField;
    public TMP_Dropdown inputTokenDropdown;
    public TMP_Dropdown outputTokenDropdown;
    public TMP_Text inputBalanceText;
    public TMP_Text outputBalanceText;
    public TMP_Text exchangeRateText;
    public TMP_Text priceImpactText;
    public TMP_Text minimumReceivedText;
    public TMP_Text networkFeeText;
    public Button swapButton;
    public Button maxButton;
    public Button reverseButton;
    public NotificationPopup notificationPopup;
    
    [Header("Swap Settings")]
    [Range(0, 1000)]
    public float slippageBps = 250; // 2.5% standard slippage
    public bool debugMode = true;

    private JObject currentQuote; 
    private Dictionary<string, TokenBalance> tokenBalances = new Dictionary<string, TokenBalance>();
    private bool isUpdatingQuote = false;

    private void Start()
    {
        jupiterBaseUrl = jupiterBaseUrl.Trim().TrimEnd('/');
        jupiterApiKey = jupiterApiKey.Trim();

        InitializeEmptyBalances();
        SetupUI();
        InvokeRepeating(nameof(RefreshBalances), 1f, 15f);
    }

    private void InitializeEmptyBalances()
    {
        tokenBalances["SOL"] = new TokenBalance { mint = wrappedSolMint, balance = 0, decimals = 9 };
        tokenBalances["USDC"] = new TokenBalance { mint = usdcMint, balance = 0, decimals = 6 };
        tokenBalances["$PLAY"] = new TokenBalance { mint = playTokenMint, balance = 0, decimals = 9 };
    }

    private void SetupUI()
    {
        if (inputAmountField != null) inputAmountField.onValueChanged.AddListener(OnInputAmountChanged);
        if (swapButton != null) swapButton.onClick.AddListener(ExecuteSwap);
        if (maxButton != null) maxButton.onClick.AddListener(SetMaxAmount);
        if (reverseButton != null) reverseButton.onClick.AddListener(ReverseTokens);
        if (inputTokenDropdown != null) inputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());
        if (outputTokenDropdown != null) outputTokenDropdown.onValueChanged.AddListener((_) => OnTokenSelectionChanged());

        SetupTokenDropdowns();
    }

    private void SetupTokenDropdowns()
    {
        var options = new List<string> { "SOL", "USDC", "$PLAY" };
        if (inputTokenDropdown != null) { inputTokenDropdown.ClearOptions(); inputTokenDropdown.AddOptions(options); inputTokenDropdown.value = 0; }
        if (outputTokenDropdown != null) { outputTokenDropdown.ClearOptions(); outputTokenDropdown.AddOptions(options); outputTokenDropdown.value = 1; }
    }

    #region BALANCE MANAGEMENT

    public async void RefreshBalances()
    {
        if (WalletConnector.UserPublicKey == null) return;
        await UpdateTokenBalance(wrappedSolMint, "SOL");
        await UpdateTokenBalance(usdcMint, "USDC");
        if (!string.IsNullOrEmpty(playTokenMint)) await UpdateTokenBalance(playTokenMint, "$PLAY");
        UpdateBalanceDisplays();
    }

    private async Task UpdateTokenBalance(string mint, string symbol)
    {
        try
        {
            if (mint == wrappedSolMint)
            {
                var result = await Web3.Rpc.GetBalanceAsync(WalletConnector.UserPublicKey);
                if (result.WasSuccessful)
                {
                    double balance = (double)result.Result.Value / 1_000_000_000;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = 9 };
                }
            }
            else
            {
                var result = await Web3.Rpc.GetTokenAccountsByOwnerAsync(WalletConnector.UserPublicKey, mint, null);
                if (result.WasSuccessful && result.Result.Value.Count > 0)
                {
                    var tokenAccount = result.Result.Value[0];
                    double balance = double.Parse(tokenAccount.Account.Data.Parsed.Info.TokenAmount.UiAmountString);
                    int decimals = tokenAccount.Account.Data.Parsed.Info.TokenAmount.Decimals;
                    tokenBalances[symbol] = new TokenBalance { mint = mint, balance = balance, decimals = decimals };
                }
            }
        }
        catch { }
    }

    private void UpdateBalanceDisplays()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        string outputToken = GetSelectedToken(outputTokenDropdown);

        if (inputBalanceText != null)
            inputBalanceText.text = tokenBalances.ContainsKey(inputToken) ? 
                $"Balance: {tokenBalances[inputToken].balance:F4} {inputToken}" : "--";

        if (outputBalanceText != null)
            outputBalanceText.text = tokenBalances.ContainsKey(outputToken) ? 
                $"Balance: {tokenBalances[outputToken].balance:F4} {outputToken}" : "--";
    }

    #endregion

    #region UI INTERACTIONS

    private void OnInputAmountChanged(string value)
    {
        if (isUpdatingQuote) return;
        if (float.TryParse(value, out float amount) && amount > 0) UpdateQuote();
        else ClearQuoteInfo();
    }

    private void OnTokenSelectionChanged()
    {
        UpdateBalanceDisplays();
        if (!string.IsNullOrEmpty(inputAmountField.text) && float.TryParse(inputAmountField.text, out float amount) && amount > 0) UpdateQuote();
        else ClearQuoteInfo();
    }

    private void SetMaxAmount()
    {
        string inputToken = GetSelectedToken(inputTokenDropdown);
        if (tokenBalances.ContainsKey(inputToken))
        {
            double maxAmount = tokenBalances[inputToken].balance;
            if (inputToken == "SOL") maxAmount = Math.Max(0, maxAmount - 0.015);
            inputAmountField.text = maxAmount.ToString("F6");
        }
    }

    private void ReverseTokens()
    {
        if (inputTokenDropdown != null && outputTokenDropdown != null)
        {
            int temp = inputTokenDropdown.value;
            inputTokenDropdown.value = outputTokenDropdown.value;
            outputTokenDropdown.value = temp;
            string tempAmount = inputAmountField.text;
            inputAmountField.text = outputAmountField.text;
            outputAmountField.text = tempAmount;
        }
    }

    #endregion

    #region QUOTE & SWAP

    private async void UpdateQuote()
    {
        if (isUpdatingQuote) return;
        isUpdatingQuote = true;

        try
        {
            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);
            
            if (inputToken == outputToken || !float.TryParse(inputAmountField.text, out float amount) || amount <= 0)
            {
                isUpdatingQuote = false; 
                return;
            }

            string inputMint = GetTokenMint(inputToken);
            string outputMint = GetTokenMint(outputToken);
            
            int decimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            ulong amountRaw = (ulong)(amount * Math.Pow(10, decimals));

            if(debugMode) Debug.Log($"[Jupiter] Fetching Quote: {amount} {inputToken} -> {outputToken}");

            // 🎯 METHOD 3: Use versioned transactions (modern approach)
            currentQuote = await GetQuoteWithRetry(inputMint, outputMint, amountRaw);

            if (currentQuote != null)
            {
                LogRouteDetails(currentQuote);
                DisplayQuoteInfo();
            }
            else 
            {
                Debug.LogError("[Jupiter] No route found!");
                ShowPopup("No Route", "Try different tokens or amount", Color.red);
                ClearQuoteInfo();
            }
        }
        catch (Exception e) 
        { 
            Debug.LogError($"[Jupiter] Quote Error: {e.Message}");
            ClearQuoteInfo(); 
        }
        finally { isUpdatingQuote = false; }
    }

    // 🎯 METHOD 3: Use Versioned Transactions - supports up to ~1400 bytes with Address Lookup Tables
    private async Task<JObject> GetQuoteWithRetry(string inputMint, string outputMint, ulong amountRaw)
    {
        // Use versioned transactions (no asLegacyTransaction flag)
        // This allows use of Address Lookup Tables for more complex routes
        int maxAccounts = 64;
        
        string url = $"{jupiterBaseUrl}/swap/v1/quote?inputMint={inputMint}&outputMint={outputMint}&amount={amountRaw}&slippageBps={slippageBps}&maxAccounts={maxAccounts}";
        
        if(debugMode) Debug.Log($"[Jupiter] Fetching versioned transaction quote (maxAccounts={maxAccounts})...");
        
        JObject quote = await GetQuote(url);
        
        if (quote != null)
        {
            if(debugMode) Debug.Log($"<color=green>[Jupiter] ✅ Versioned transaction quote received</color>");
            return quote;
        }
        
        Debug.LogError("[Jupiter] No route found!");
        return null;
    }

    private void LogRouteDetails(JObject quote)
    {
        try 
        {
            JArray routePlan = (JArray)quote["routePlan"];
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>[Jupiter Debug] Route Plan:</b>");
            int hopCount = 0;
            foreach (var hop in routePlan)
            {
                hopCount++;
                string label = hop["swapInfo"]["label"].ToString();
                string ammKey = hop["swapInfo"]["ammKey"].ToString();
                sb.AppendLine($"  Hop {hopCount}: <b>{label}</b> (AMM: {ammKey.Substring(0, 5)}...)");
            }
            Debug.Log(sb.ToString());
        }
        catch { }
    }

    private void DisplayQuoteInfo()
    {
        if (currentQuote == null) return;

        try
        {
            if (swapButton != null) swapButton.interactable = true;

            string inputToken = GetSelectedToken(inputTokenDropdown);
            string outputToken = GetSelectedToken(outputTokenDropdown);
            
            int inDecimals = tokenBalances.ContainsKey(inputToken) ? tokenBalances[inputToken].decimals : 9;
            int outDecimals = tokenBalances.ContainsKey(outputToken) ? tokenBalances[outputToken].decimals : 9;

            double inAmount = double.Parse(currentQuote["inAmount"].ToString()) / Math.Pow(10, inDecimals);
            double outAmount = double.Parse(currentQuote["outAmount"].ToString()) / Math.Pow(10, outDecimals);
            double minReceived = double.Parse(currentQuote["otherAmountThreshold"].ToString()) / Math.Pow(10, outDecimals);

            if (outputAmountField != null) outputAmountField.text = outAmount.ToString($"F{Math.Min(6, outDecimals)}");

            if (exchangeRateText != null && inAmount > 0)
            {
                double rate = outAmount / inAmount;
                exchangeRateText.text = $"1 {inputToken} ≈ {rate:F4} {outputToken}";
            }

            if (priceImpactText != null && currentQuote["priceImpactPct"] != null)
            {
                double impact = double.Parse(currentQuote["priceImpactPct"].ToString()) * 100;
                priceImpactText.text = $"{impact:F2}%";
                if (impact < 0.1) priceImpactText.color = Color.green;
                else if (impact < 1.0) priceImpactText.color = new Color(1f, 0.64f, 0f);
                else priceImpactText.color = Color.red;
            }

            if (minimumReceivedText != null) minimumReceivedText.text = $"{minReceived:F4} {outputToken}";
            if (networkFeeText != null) networkFeeText.text = "~0.000005 SOL (Auto)";
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Jupiter] UI Error: {ex.Message}");
        }
    }

    private void ClearQuoteInfo()
    {
        if (outputAmountField != null) outputAmountField.text = "";
        if (exchangeRateText != null) exchangeRateText.text = "--";
        if (priceImpactText != null) priceImpactText.text = "--";
        if (minimumReceivedText != null) minimumReceivedText.text = "--";
        if (networkFeeText != null) networkFeeText.text = "--";
        if (swapButton != null) swapButton.interactable = false;
    }

    private async void ExecuteSwap()
    {
        if (currentQuote == null || WalletConnector.UserPublicKey == null) return;
        
        swapButton.interactable = false;
        ShowPopup("Creating Transaction", "Please wait...", Color.yellow);

        try
        {
            string txBase64 = await GetSwapTransaction(currentQuote);
            if (string.IsNullOrEmpty(txBase64)) 
            { 
                ShowPopup("Error", "Failed to create transaction", Color.red); 
                return; 
            }

            byte[] bytes = Convert.FromBase64String(txBase64);
            if(debugMode) Debug.Log($"<b>[Jupiter Debug] Versioned Transaction Size: {bytes.Length} bytes</b>");

            ShowPopup("Confirm Swap", "Check your wallet", Color.yellow);
            await SignAndSendTransaction(txBase64);
            
            await Task.Delay(2000);
            RefreshBalances();
        }
        catch (Exception ex) 
        { 
            ShowPopup("Failed", ex.Message, Color.red); 
            Debug.LogError($"[Jupiter] Swap execution error: {ex.Message}");
        }
        finally { swapButton.interactable = true; }
    }

    private async Task<JObject> GetQuote(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);

            await req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success)
            {
                return JObject.Parse(req.downloadHandler.text); 
            }
            else
            {
                if(debugMode) Debug.LogWarning($"[Jupiter] API Response: {req.error}");
                return null;
            }
        }
    }

    private async Task<string> GetSwapTransaction(JObject quote)
    {
        var reqData = new 
        {
            quoteResponse = quote, 
            userPublicKey = WalletConnector.UserPublicKey.ToString(),
            wrapAndUnwrapSol = true,
            prioritizationFeeLamports = 0,
            useSharedAccounts = false
            // 🎯 NO asLegacyTransaction - this enables versioned transactions
        };

        string json = JsonConvert.SerializeObject(reqData);
        
        using (UnityWebRequest req = new UnityWebRequest($"{jupiterBaseUrl}/swap/v1/swap", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            
            req.SetRequestHeader("Content-Type", "application/json");
            if(!string.IsNullOrEmpty(jupiterApiKey)) req.SetRequestHeader("x-api-key", jupiterApiKey);

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var responseObj = JObject.Parse(req.downloadHandler.text);
                return responseObj["swapTransaction"].ToString();
            }
            else
            {
                Debug.LogError($"[Jupiter] Swap Error: {req.error} - {req.downloadHandler.text}");
                return null;
            }
        }
    }

    private async Task SignAndSendTransaction(string txBase64)
    {
        try
        {
            if(debugMode) Debug.Log($"[Jupiter] Received versioned transaction (base64 length: {txBase64.Length})");
            
            if (WalletConnector.PlayerAccount != null) 
            {
                if(debugMode) Debug.Log("[Jupiter] Signing versioned transaction with local account...");
                
                byte[] txBytes = Convert.FromBase64String(txBase64);
                
                // 🎯 VERSIONED TRANSACTION PARSING
                // Versioned transactions start with a version byte (0x80 for v0)
                int offset = 0;
                byte versionByte = txBytes[offset++];
                
                bool isVersioned = (versionByte & 0x80) != 0;
                
                if(debugMode) Debug.Log($"[Jupiter] Transaction is {(isVersioned ? "VERSIONED" : "LEGACY")}");
                
                if (isVersioned)
                {
                    // For versioned transactions, the structure is:
                    // [version (1 byte)][num_signatures (compact-u16)][64 zero bytes per sig][message]
                    
                    // Read compact-u16 for number of signatures
                    int numSignatures = ReadCompactU16(txBytes, ref offset);
                    
                    // Skip signature placeholders
                    int messageStart = offset + (numSignatures * 64);
                    
                    // Extract message bytes
                    byte[] messageBytes = new byte[txBytes.Length - messageStart];
                    Array.Copy(txBytes, messageStart, messageBytes, 0, messageBytes.Length);
                    
                    if(debugMode) Debug.Log($"[Jupiter] Versioned TX - Signatures needed: {numSignatures}, Message size: {messageBytes.Length} bytes");
                    
                    // Sign the message
                    byte[] signature = WalletConnector.PlayerAccount.Sign(messageBytes);
                    
                    // Reconstruct: [version][num_sigs][signature(s)][message]
                    byte[] signedTx = new byte[1 + GetCompactU16Size(numSignatures) + (signature.Length * numSignatures) + messageBytes.Length];
                    int writeOffset = 0;
                    
                    signedTx[writeOffset++] = versionByte;
                    WriteCompactU16(signedTx, ref writeOffset, numSignatures);
                    Array.Copy(signature, 0, signedTx, writeOffset, signature.Length);
                    writeOffset += signature.Length;
                    Array.Copy(messageBytes, 0, signedTx, writeOffset, messageBytes.Length);
                    
                    string signedTxBase64 = Convert.ToBase64String(signedTx);
                    
                    if(debugMode) Debug.Log($"[Jupiter] Signed versioned transaction: {signedTx.Length} bytes");
                    
                    await SendSignedTransaction(signedTxBase64);
                }
                else
                {
                    // Legacy transaction handling (same as before)
                    int numSignatures = txBytes[0];
                    int messageStart = 1 + (numSignatures * 64);
                    
                    byte[] messageBytes = new byte[txBytes.Length - messageStart];
                    Array.Copy(txBytes, messageStart, messageBytes, 0, messageBytes.Length);
                    
                    byte[] signature = WalletConnector.PlayerAccount.Sign(messageBytes);
                    
                    byte[] signedTx = new byte[1 + signature.Length + messageBytes.Length];
                    signedTx[0] = (byte)numSignatures;
                    Array.Copy(signature, 0, signedTx, 1, signature.Length);
                    Array.Copy(messageBytes, 0, signedTx, 1 + signature.Length, messageBytes.Length);
                    
                    string signedTxBase64 = Convert.ToBase64String(signedTx);
                    
                    if(debugMode) Debug.Log($"[Jupiter] Signed legacy transaction: {signedTx.Length} bytes");
                    
                    await SendSignedTransaction(signedTxBase64);
                }
            } 
            else 
            {
                if(debugMode) Debug.Log("[Jupiter] Signing with external wallet...");
                
                // For external wallets, let the wallet handle it
                byte[] txBytes = Convert.FromBase64String(txBase64);
                var tx = Transaction.Deserialize(txBytes);
                var res = await Web3.Wallet.SignAndSendTransaction(tx);

                if (res.WasSuccessful) 
                {
                    ShowPopup("Success!", "Swap Completed!", Color.green);
                    if(debugMode) Debug.Log($"[Jupiter] TX Sent: {res.Result}");
                    await Task.Delay(4000);
                    RefreshBalances();
                }
                else 
                {
                    ShowPopup("Failed", $"RPC Error: {res.Reason}", Color.red);
                    Debug.LogError($"[Jupiter] TX Failed: {res.RawRpcResponse}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowPopup("Error", "Signing Failed", Color.red);
            Debug.LogError($"[Jupiter] Signing Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task SendSignedTransaction(string signedTxBase64)
    {
        using (UnityWebRequest req = new UnityWebRequest(Web3.Rpc.NodeAddress.AbsoluteUri, "POST"))
        {
            var rpcRequest = new 
            {
                jsonrpc = "2.0",
                id = UnityEngine.Random.Range(1, 10000),
                method = "sendTransaction",
                @params = new object[] 
                { 
                    signedTxBase64,
                    new 
                    {
                        encoding = "base64",
                        skipPreflight = false,
                        preflightCommitment = "confirmed"
                    }
                }
            };

            string json = JsonConvert.SerializeObject(rpcRequest);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                var response = JObject.Parse(req.downloadHandler.text);
                
                if (response["error"] != null)
                {
                    ShowPopup("Failed", "Transaction Error", Color.red);
                    Debug.LogError($"[Jupiter] TX Failed: {response["error"]}");
                }
                else
                {
                    string txSignature = response["result"].ToString();
                    ShowPopup("Success!", "Swap Completed!", Color.green);
                    if(debugMode) Debug.Log($"[Jupiter] TX Sent: {txSignature}");
                    await Task.Delay(4000);
                    RefreshBalances();
                }
            }
            else
            {
                ShowPopup("Failed", "Network Error", Color.red);
                Debug.LogError($"[Jupiter] Request Failed: {req.error}");
            }
        }
    }

    // 🎯 COMPACT-U16 ENCODING UTILITIES (for versioned transactions)
    private int ReadCompactU16(byte[] data, ref int offset)
    {
        byte firstByte = data[offset++];
        
        if (firstByte <= 0x7f)
        {
            return firstByte;
        }
        else if (firstByte <= 0xbf)
        {
            int secondByte = data[offset++];
            return ((firstByte & 0x3f) << 8) | secondByte;
        }
        else
        {
            int secondByte = data[offset++];
            int thirdByte = data[offset++];
            return ((firstByte & 0x1f) << 16) | (secondByte << 8) | thirdByte;
        }
    }

    private int GetCompactU16Size(int value)
    {
        if (value <= 0x7f) return 1;
        if (value <= 0x3fff) return 2;
        return 3;
    }

    private void WriteCompactU16(byte[] data, ref int offset, int value)
    {
        if (value <= 0x7f)
        {
            data[offset++] = (byte)value;
        }
        else if (value <= 0x3fff)
        {
            data[offset++] = (byte)(0x80 | (value >> 8));
            data[offset++] = (byte)(value & 0xff);
        }
        else
        {
            data[offset++] = (byte)(0xc0 | (value >> 16));
            data[offset++] = (byte)((value >> 8) & 0xff);
            data[offset++] = (byte)(value & 0xff);
        }
    }
    
    #endregion

    #region HELPERS & CLASSES
    private string GetSelectedToken(TMP_Dropdown d) => d == null ? "SOL" : d.options[d.value].text;
    private string GetTokenMint(string t) => t == "SOL" ? wrappedSolMint : (t == "USDC" ? usdcMint : playTokenMint);
    private void ShowPopup(string t, string m, Color c) { if (notificationPopup != null) notificationPopup.Show(t, m, c); }

    [Serializable] public class TokenBalance { public string mint; public double balance; public int decimals; }
    #endregion
}