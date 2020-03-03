require 'pp'
require 'fileutils'
require 'open3'
require 'optparse'

MY_LANG_EXE = ["dotnet", "MyLang/bin/Debug/netcoreapp2.1/MyLang.dll"]

def run_test(test_name, testcases, cmd)
  puts "** Testing #{test_name} ..." if $verbose
  total = 0
  success = 0

  testcases.each do |input, expected_output|
    total += 1
    output, status = Open3.capture2e(*cmd, input)
    output.strip!

    if status.exitstatus != 0
      puts "ERROR: #{input} fail"
      puts output
      next
    end
    
    if output != expected_output.to_s
      puts "NG: #{input} => #{output}, but expect #{expected_output}"
    else
      success += 1
      if $verbose
        puts "OK: #{input} => #{output}" 
      else
        print "."
      end
    end
  end

  puts unless $verbose

  if total == success
    puts "OK: #{total} testcases passed" if $verbose
  else
    puts "ERR: #{total-success} testcases failed"
  end
end

def test_tokenizer
  testcases = [
    ["1", "1 [EOF]"],
    ["1 + 2", "1 + 2 [EOF]"],
    ["1   +   2", "1 + 2 [EOF]"],
    ["1   +  2 * 3", "1 + 2 * 3 [EOF]"],
    ["1+2", "1 + 2 [EOF]"], # スペースがなくても、Tokenizeできるようにする
    ["a + b", "a + b [EOF]"], # Symbolも対応する
    ["(1 + 2) * 3", "( 1 + 2 ) * 3 [EOF]"], # "(", ")" に対応する
    ["< <= >= == !=", "< <= >= == != [EOF]"],
  ]
  run_test("Tokenizer", testcases, [*MY_LANG_EXE, '--tokenize', '-c'])
end


def test_parser
  testcases = [
    ["1;", "1"],
    ["1 + 2;", "Add( 1 2 )"],
    ["2 * 3;", "Multiply( 2 3 )"],
    ["1 + 2 * 3;", "Add( 1 Multiply( 2 3 ) )"],
    ["1 + 2 + 3;", "Add( Add( 1 2 ) 3 )"],
    ["1 * 2 * 3;", "Multiply( Multiply( 1 2 ) 3 )"],
  ]
  run_test("Parser", testcases, [*MY_LANG_EXE, '--parse', '-c'])
end

def test_interpreter
  testcases = [
    ["1", 1],
    ["1 + 2", 3],
    ["2 * 3", 6],
    ["1 + 2 * 3", 7],
    ["1 + 2 + 3", 6],
    ["1 * 2 * 3", 6],
  ]
  run_test("Interpreter", testcases, [*MY_LANG_EXE, '-c'])
end

def test_interpreter2
  testcases = [
    ["let a = 1; print a;", "1"],
    ["let a = 1; if a == 1 { print 1; } else { print 0; }", "1"],
    ["let a = 1; if a == 0 { print 1; } else { print 0; }", "0"],
    ["let a = 1; if a < 0 { print 1; } else { print 0; }", "0"],
    ["let a = 1; if a > 0 { print 1; } else { print 0; }", "1"],
  ]
  run_test("Interpreter", testcases, [*MY_LANG_EXE, '-e'])
end

$verbose = false
op = OptionParser.new
op.on('v','--verbose','show log'){|v| $verbose = true }
op.parse!

test_tokenizer
test_parser
test_interpreter
test_interpreter2
